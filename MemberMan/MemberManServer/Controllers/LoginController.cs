
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using officepark.io.API;
using officepark.io.Membership;

namespace MemberMan;

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
[ApiController]
[Route("[controller]")]
public class LoginController : ApiController
{
  public const int INVALID_VERIFICATION = 1;
  public const int VERIFICATION_EXPIRED = 2;

  // --------------------------------------------------------------------------------------------------------------------------
  private IMemberAccess _DAL = default!;
  private IEmailService _Email = default!;
  public LoginController(IMemberAccess dal_, IEmailService email_)
  {
    if (dal_ == null) { throw new ArgumentNullException("dal_"); }
    if (email_ == null) { throw new ArgumentNullException("email_"); }

    _DAL = dal_;
    _Email = email_;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// M$FT hates us and makes model binding as hard as possible.
  /// We can't just bind single properties from a POST request, we instead have to make a composite type.
  /// </summary>
  public class VerificationArgs
  {
    public string Username { get; set; } = default!;
    public string? VerificationCode { get; set; } = default!;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will request that the system re-send the verification email (or whatever).
  /// The response is always 200 as this function is not meant to indicate whether the user actually exists or not.
  /// </summary>
  [HttpPost]
  [Route("/api/verify")]
  public IAPIResponse RequestVerification([FromBody] VerificationArgs args)
  {
    var member = _DAL.GetMemberByName(args.Username);
    if (member != null)
    {
      SendVerificationMessage(member);
    }

    return new BasicResponse()
    {
      Message = "OK"
    };
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will create a new, unverifed user in the system.
  /// An email or something will be sent out so that the user may verify.
  /// NOTE: We could also get into that cellphone verification stuff too!
  /// </summary>
  [HttpPost]
  [Route("/api/signup")]
  public SignupResponse Signup(LoginModel login)
  {
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

      Member m = _DAL.CreateMember(login.username, login.email, login.password);

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
  private Email CreateVerificationEmail(Member m)
  {
    var sb = new StringBuilder();

    const string DOMAIN = "";
    sb.Append($"<p>Your verification code is: <a class=\"verify-link\" href=\"/{DOMAIN}/api/verifyuser?code={m.VerificationCode}\">Click Here to Verify your Account</a></p>");
    var res = new Email()
    {
      Body = sb.ToString()
    };
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpGet]
  [Route("/api/verifyuser")]
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
      if (m.VerificationExpiration == null || now > m.VerificationExpiration)
      {
        m = _DAL.RefreshVerification(m.Username);

        res.Code = VERIFICATION_EXPIRED;
        res.Message = "Verification is expired.  A new verification email will be sent.";

        SendVerificationMessage(m);

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
      return res;
    }

    // Set the auth token cookie too?
    // NOTE: Here we can interprt options to decide if the user can be logged in, even if they aren't verifed.
    string msg = "OK";
    bool isVerified = member.VerifiedOn != null;
    if (!isVerified)
    {
      msg = $"User: {member.Username} is not verified.";
    }

    bool isLoggedIn = isVerified;

    return new LoginResponse()
    {
      IsLoggedIn = isLoggedIn,
      IsVerified = isVerified,
      AuthRequired = true,
      Message = msg,
      DisplayName = login.username,
    };
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

}
