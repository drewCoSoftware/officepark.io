
using System.Runtime.CompilerServices;
using System.Text;
using DotLiquid;
using drewCo.Tools;
using MemberManServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Routing;
using officepark.io.API;
using officepark.io.Membership;
using static MemberManServer.Mailer;
using IOFile = System.IO.File;

namespace MemberMan;


// ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class LoginController : ApiController
{
  public const int INVALID_VERIFICATION = 0x11;
  public const int VERIFICATION_EXPIRED = 0x12;
  public const int NOT_VERFIED = 0x13;
  public const int LOGIN_FAILED = 0x14;



  private IMemberAccess _DAL = default!;
  private IEmailService _Email = default!;
  private ConfigHelper _Config = null!;

  private MemberManConfig MemberManCfg = null!;

  // --------------------------------------------------------------------------------------------------------------------------
  public LoginController(IMemberAccess dal_, IEmailService email_, ConfigHelper config_)
  {
    if (dal_ == null) { throw new ArgumentNullException("dal_"); }
    if (email_ == null) { throw new ArgumentNullException("email_"); }

    _DAL = dal_;
    _Email = email_;
    _Config = config_;

    MemberManCfg = _Config.Get<MemberManConfig>();
  }



  // --------------------------------------------------------------------------------------------------------------------------
  [HttpGet]
  [Route("/api/verify")]
  public BasicResponse VerifyUser(string code)
  {
    var res = new BasicResponse()
    {
      Code = 0,
      Message = "OK"
    };

    Member? m = _DAL.GetMemberByVerification(code);
    if (m == null)
    {
      res.Code = INVALID_VERIFICATION;
      res.Message = "Invalid verification code";
    }
    else
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      if (now > m.VerificationExpiration)
      {

        res.Code = VERIFICATION_EXPIRED;
        res.Message = "Verification code is expired.";

        // OPTION: Auto-reverify?  I don't know seems dangerous and we shouldn't let bots do it....
        // m = _DAL.RefreshVerification(m.Username, MemberManConfig.DEFAULT_VERIFY_WINDOW);
        // SendVerificationMessage(m);

        return res;
      }

      _DAL.CompleteVerification(m, now);
      res.Code = 0;
      res.Message = "OK";
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/login")]
  public LoginResponse Login(LoginModel login)
  {

    // Reach into the DAL to look for active user + password.
    Member? member = _DAL.CheckLogin(login.username, login.password);
    if (member == null)
    {
      // NOTE: This should return a 404!
      var res = NotFound<LoginResponse>("Invalid username or password!");
      res.Code = LoginController.LOGIN_FAILED;
      return res;
    }

    // Set the auth token cookie too?
    // NOTE: Here we can interprt options to decide if the user can be logged in, even if they aren't verifed.
    string msg = "OK";
    int code = 0;
    bool isVerified = member.VerifiedOn != null;
    if (!isVerified)
    {
      msg = $"User: {member.Username} is not verified.";
      code = LoginController.NOT_VERFIED;
    }

    // TODO: OPTIONS:
    bool isLoggedIn = true;
    bool ALLOW_UNVERIFIED_LOGIN = false;
    if (!isVerified && !ALLOW_UNVERIFIED_LOGIN)
    {
      isLoggedIn = false;
    }
    return new LoginResponse()
    {
      IsLoggedIn = isLoggedIn,
      IsVerified = isVerified,
      AuthRequired = true,
      Message = msg,
      DisplayName = login.username,
      Code = code
    };
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will request that the system re-send the verification email (or whatever).
  /// The response is always 200 as this function is not meant to indicate whether the user actually exists or not.
  /// </summary>
  [HttpPost]
  [Route("/api/reverify")]
  public IAPIResponse RequestVerification([FromBody] VerificationArgs args)
  {
    // TODO: Some kind of cookie check to make sure that the user was actually directed to reverify!
    // That means a one-time response cookie from the login, and then we should be handing that cookie off to this request...
    // NOTE: That kind of handshake is kind of advanced, and may not be needed....


    // TODO: A logged in user should get a 404 or some other error for this ?
    var member = _DAL.GetMemberByName(args.Username);
    if (member != null)
    {
      // NOTE: A user that is already verified shouldn't be here anyway, so we aren't
      // going to indicate that anything is amiss.
      if (!member.IsVerified)
      {
        member = _DAL.RefreshVerification(member.Username, MemberManCfg.VerifyWindow);
        SendVerificationMessage(member);
      }
    }

    return new BasicResponse()
    {
      Message = "OK"
    };
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will create a new, unverifed member in the system.
  /// An email or something will be sent out so that the member may verify their account.
  /// NOTE: We could also get into that cellphone verification stuff too!
  /// </summary>
  [HttpPost]
  [Route("/api/signup")]
  public SignupResponse Signup(LoginModel login)
  {
    // TEST:  How can we test attributes / filters in netcore? (we would have to pass cookies around....)
    // TODO: A logged in user should get a 404 or some other error for this... (is there a 200 level code that can articulate this correctly?  are we getting ballz deep into semantics?)
    // The return code should also indicate that the user is already logged in.....

    ValidateLoginData(login);

    MemberAvailability availability = _DAL.CheckAvailability(login.username, login.email);
    bool isAvailable = availability.IsUsernameAvailable && availability.IsEmailAvailable;

    if (!isAvailable)
    {
      var msgs = new List<string>();
      // NOTE: By default we only report when an email address is not available.
      //if (!availability.IsUsernameAvailable)
      //{
      //  msgs.Add($"The username '{login.username}' is in use!");
      //}
      if (!availability.IsEmailAvailable)
      {
        msgs.Add($"The email address '{login.email}' is in use!");
      }

      return new SignupResponse()
      {
        IsUsernameAvailable = isAvailable,
        Message = string.Join('\n', msgs),
        Code = 409    // (use 409 response code too?)
      };
    }

    // In test scenarios we don't actually create the user account.
    // NOTE: 'Request' is null when we are running unit tests.  There may be a better way to wrap the
    // code that gets the headers so that we can test them too.
    if (!HasHeader("X-Test-Api-Call"))
    {

      Member m = _DAL.CreateMember(login.username, login.email, login.password, MemberManCfg.VerifyWindow);

      // This is where we will send out the verification, etc. emails.
      SendVerificationMessage(m);
    }

    return new SignupResponse()
    {
      IsUsernameAvailable = true,
      AuthRequired = false,
      Message = "Signup OK!",
    };

  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected bool HasHeader(string headerName)
  {
    if (Request == null) { return false; }
    bool res = Request.Headers.ContainsKey(headerName);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected void ValidateLoginData(LoginModel login)
  {
    login.username = login.email;
    if (!StringTools_Local.IsValidEmail(login.email))
    {
      throw new InvalidOperationException("Invalid email address!");
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SendVerificationMessage(Member member)
  {
    Email email = CreateVerificationEmail(member);
    this._Email.SendEmail(email);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual Email CreateVerificationEmail(Member m)
  {
    string templateText = IOFile.ReadAllText(Path.Combine(FileTools.GetLocalDir("EmailTemplates"), "Verification.html"));


    var mmCfg = _Config.Get<MemberManConfig>();
    string link = mmCfg.VerificationUrl + $"?code={m.VerificationCode}";

    // var date = new DateTimeOffset( m.VerificationExpiration
    // TODO: Localize to EST and include that in the email.
    string expires = m.VerificationExpiration.ToString("MM/dd/yyyy at hh:mm:ss");

    var model = new
    {
      VerificationLink = link,
      VerificationCode = m.VerificationCode,
      ExpirationTime = expires
    };

    var t = Template.Parse(templateText);
    string final = t.Render(Hash.FromAnonymousObject(new { model = model }));
    Console.WriteLine(final);


    var res = new Email(MemberManCfg.VerificationSender, m.Email, "Verify your account!", final, true);
    return res;
  }


}


// ============================================================================================================================
/// <summary>
/// M$FT hates us and makes model binding as hard as possible.
/// We can't just bind single properties from a POST request, we instead have to make a composite type.
/// </summary>
public class VerificationArgs
{
  public string Username { get; set; } = default!;
  public string? VerificationCode { get; set; } = default!;
}

// ============================================================================================================================
// TODO: Move this functionality to drewCo.Tools.
public class StringTools_Local
{

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Tells us if the given email address is valid or not.
  /// </summary>
  /// <remarks>
  /// Email addres validation is difficult.  This function may not cover all cases.
  /// Please report any valid email address that causes this function to return false.
  /// </remarks>
  public static bool IsValidEmail(string email)
  {
    // Thanks Internet!
    // Original version from:
    // https://stackoverflow.com/questions/1365407/c-sharp-code-to-validate-email-address

    var trimmedEmail = email.Trim();

    if (trimmedEmail.EndsWith("."))
    {
      return false; // suggested by @TK-421
    }
    try
    {
      var addr = new System.Net.Mail.MailAddress(email);
      return addr.Address == trimmedEmail;
    }
    catch
    {
      return false;
    }
  }
}


// ============================================================================================================================
public class LoginResponse : BasicResponse
{
  /// <summary>
  /// Is the user logged in?
  /// </summary>
  public bool IsLoggedIn { get; set; }

  /// <summary>
  /// Is this a verified user?  Depending on the application, the user may or may not be allowed to 
  /// access certain features or even the entire system.
  /// </summary>
  public bool IsVerified { get; set; }

  /// <summary>
  /// The name that should be displayed in a UI.  This doesn't have to be the same thing
  /// as the username used on login.
  /// </summary>
  public string DisplayName { get; set; } = default!;

  /// <summary>
  /// Url to user avatar.  Can be an image, gravatar, whatever....
  /// </summary>
  public string? Avatar { get; set; } = null;
}

// ============================================================================================================================
public class LoginModel
{
  /// <summary>
  /// All users have an associated email address so that there is at least one way to attempt contact.
  /// It is perfectly acceptable to use the email address as the user name.  In theses cases, simply
  /// set username == email.
  /// </summary>
  /// <remarks>We may remove email from this model class.... It technically isn't used for logins....</remarks>
  public string email { get; set; } = string.Empty;

  public string username { get; set; } = string.Empty;
  public string password { get; set; } = string.Empty;
}

// ============================================================================================================================
public class SignupResponse : BasicResponse
{
  public bool IsUsernameAvailable { get; set; }
  public bool IsEmailAvailable { get; set; }
}


// ============================================================================================================================
/// <summary>
/// Configuration for member man and its features.
/// </summary>
public class MemberManConfig
{
  public static readonly TimeSpan DEFAULT_VERIFY_WINDOW = TimeSpan.FromHours(24);

  /// <summary>
  /// The url that the user should visit to verify their account.
  /// </summary>
  public string VerificationUrl { get; set; } = default!;


  // public string Domain { get; set; } = default!;

  /// <summary>
  /// Email account that sends verification emails.
  /// </summary>
  public string VerificationSender { get; set; } = default!;

  /// <summary>
  /// The server address that emails are sent through....
  /// </summary>
  public string SmtpServer { get; set; } = default!;
  public int SmtpPort { get; set; } = 465;
  public string SmtpPassword { get; set; } = default!;

  public TimeSpan VerifyWindow { get; set; } = DEFAULT_VERIFY_WINDOW;
}


// ============================================================================================================================
public static class Cookies
{
  public const string REVERIFY = "reverifytoken";

}

