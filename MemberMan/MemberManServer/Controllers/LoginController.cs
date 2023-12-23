
using System.Runtime.CompilerServices;
using System.Text;
using DotLiquid;
using drewCo.Tools;
using Humanizer;
using Humanizer.Localisation;
using MemberManServer;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Net.Http.Headers;
using officepark.io;
using officepark.io.API;
using officepark.io.Membership;
using static MemberManServer.Mailer;
using IOFile = System.IO.File;

namespace MemberMan;


// ============================================================================================================================
/// <summary>
/// Indicates that a controller has certain member man features.
/// </summary>
internal interface IMemberManFeatures
{
  IMemberAccess DAL { get; }
  MemberManConfig MemberManConfig { get; }
}

// ============================================================================================================================
public static class EmailTemplateNames
{
  public const string VERIFICATION_TEMPLATE = "Verification";
  public const string FORGOT_PASSWORD_TEMPLATE = "ForgotPassword";
}

// ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class LoginController : ApiController, IMemberManFeatures
{
  // TODO: Wrap these into their own static class  / enum.
  public const int INVALID_VERIFICATION = 0x11;
  public const int VERIFICATION_EXPIRED = 0x12;
  public const int NOT_VERFIED = 0x13;
  public const int LOGIN_FAILED = 0x14;
  public const int INVALID_RESET_TOKEN = 0x15;
  public const int RESET_TOKEN_EXPIRED = 0x16;

  /// <summary>
  /// The user is already logged in.
  /// </summary>
  public const int LOGGED_IN = 0x17;


  public IMemberAccess DAL { get; private set; } = default!;
  public MemberManConfig MemberManConfig { get; private set; } = null!;

  private IEmailService _Email = default!;
  private ConfigHelper _ConfigHelper = null!;


  // --------------------------------------------------------------------------------------------------------------------------
  public LoginController(IMemberAccess dal_, IEmailService email_, ConfigHelper config_)
  {
    if (dal_ == null) { throw new ArgumentNullException("dal_"); }
    if (email_ == null) { throw new ArgumentNullException("email_"); }

    DAL = dal_;
    _Email = email_;
    _ConfigHelper = config_;

    MemberManConfig = _ConfigHelper.Get<MemberManConfig>();
  }


  #region Properties 

  protected string _IPAddress = default!;
  public virtual string IPAddress
  {
    get
    {
      return _IPAddress ?? (_IPAddress = IPHelper.GetIP(Request));
    }
    internal set { _IPAddress = value; }
  }

  protected string? _LoginToken = default!;
  public virtual string? LoginToken
  {
    get
    {
      return _LoginToken ?? (_LoginToken = MembershipHelper.GetLoginToken(Request));
    }
    internal set { _LoginToken = value; }
  }

  //  protected string? _MembershipCookie = default!;
  public virtual string? MembershipCookie
  {
    get
    {
      return GetCookie(MembershipHelper.MEMBERSHIP_COOKIE);
    }

    internal set
    {
      if (value != null)
      {
        SetCookie(MembershipHelper.MEMBERSHIP_COOKIE, value, DateTime.Now + TimeSpan.FromMinutes(MembershipHelper.LOGIN_COOKIE_TIME));
      }
      else
      {
        RemoveCookie(MembershipHelper.MEMBERSHIP_COOKIE);
      }

    }
  }

  #endregion




  // --------------------------------------------------------------------------------------------------------------------------
  public IMemberAccess GetDAL()
  {
    return DAL;
  }

  public MemberManConfig GetConfig() { return MemberManConfig; }


  // --------------------------------------------------------------------------------------------------------------------------
  // NOTE: This call will always work, we don't need to know if someone is logged in or not....
  [HttpPost]
  [Route("/api/logout")]
  public IAPIResponse Logout()
  {
    MembershipHelper.Logout(Request, Response);
    _LoginToken = null;

    var res = OK<BasicResponse>();
    return res;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will initiate the password reset process for a user.
  /// Note that a user may still log into their account if they remember the password
  /// before going through the whole process, which should invalidate any data that was
  /// initiated during this step.
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  [Route("/api/forgot-password")]
  public IAPIResponse ForgotPassword([FromBody] ForgotPasswordArgs args)
  {
    // TODO: Check for and reject if the user is currently logged in.
    if (IsLoggedIn())
    {
      // TODO: Research a better error code to return....
      return NotFound();
    }

    // Find the user.
    var member = DAL.GetMember(args.Username);
    if (member == null)
    {
      return NotFound();
    }

    // If they exist, then we will generate a reset token/code.
    // The DB must be updated at this point.
    string resetToken = GeneratePasswordResetToken();
    DateTimeOffset tokenExpires = DateTimeOffset.UtcNow + MemberManConfig.PasswordResetWindow;
    DAL.SetPasswordResetData(args.Username, resetToken, tokenExpires);

    // With that token, we will generate and send out an email with the reset instructions.
    // Then the email is sent!
    SendForgotPasswordMessage(member, resetToken);

    // NOTE: We always return OK for this function.
    return OK();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private string GeneratePasswordResetToken()
  {
    // TODO: Some kind of crypto / random hash or something?
    // NOTE: This should be a plugin type function so that users may define their own algos.....
    var uuid = Guid.NewGuid().ToString();
    string res = StringTools.ComputeSHA1(uuid);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private Member? GetLoggedInMember()
  {
    string? token = MembershipHelper.GetLoginToken(MembershipCookie, IPAddress);
    var res = MembershipHelper.GetMember(token);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // TODO: This could also go with some kind of base class?
  private bool IsLoggedIn()
  {
    bool res = MembershipHelper.IsLoggedIn(MembershipCookie, IPAddress);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private bool IsLoggedIn(out Member? m)
  {
    m = GetLoggedInMember();
    return m != null;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/reset-password")]
  public IAPIResponse ResetPassword([FromBody] ResetPasswordArgs args)
  {
    // TODO: Disallow logged in users.
    if (IsLoggedIn())
    {
      return NotFound();
    }

    // Make sure that the passwords match....
    // This can be part of some better defined validation type code.....
    if (args.NewPassword != args.ConfirmPassword)
    {
      return Error(-1, "Passwords do not match!");
    }

    // Get the member with the given token.
    var member = DAL.GetMemberByResetToken(args.ResetToken);
    if (member == null || member.TokenExpires == null || member.ResetToken == null)
    {
      return new BasicResponse()
      {
        Code = INVALID_RESET_TOKEN,
        Message = "Invalid reset token!"
      };
    }

    var timestamp = DateTimeOffset.Now;
    int code = 0;
    string msg = string.Empty;
    if (member.ResetToken != args.ResetToken)
    {
      code = INVALID_RESET_TOKEN;
      msg = "Reset token mismatch!";
    }

    if (timestamp > member.TokenExpires)
    {
      code = RESET_TOKEN_EXPIRED;
      msg = "Reset token expired!";
    }


    if (code != 0)
    {
      // We were not able to reset the password.....
      return Error(code, msg);
    }
    else
    {
      // Reset OK!
      DAL.RemovePasswordResetData(member.Username);
      DAL.SetPassword(member.Username, args.NewPassword);
      return OK();
    }
  }



  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This is a simple way to validate that the user is currently logged in.
  /// It will return 200 and minimal information if they are, or 404 if they are not.
  /// </summary>
  [HttpGet]
  [Route("/api/login/validate")]
  public LoginResponse ValidateLogin()
  {
    if (IsLoggedIn(out Member? m))
    {
      var res = OK<LoginResponse>();

      res.DisplayName = m!.Email;
      res.IsLoggedIn = true;
      res.Avatar = null;

      return res;
    }
    else
    {
      return NotFound<LoginResponse>("You are not logged in!");
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/login")]
  public LoginResponse Login(LoginModel login)
  {
    // If the user is currently logged in, then we can just return OK or something....
    // TODO: Can we make this block of code part of the 'CheckMembership' attribute?
    // TODO: This is also something that should use the properties for LoginToken, IPAddress, etc.
    if (IsLoggedIn())
    {
      var res = OK<LoginResponse>();
      res.IsLoggedIn = true;
      res.Code = LOGGED_IN;
      return res;
    }

    // Reach into the DAL to look for active user + password.
    Member? m = DAL.GetMember(login.username, login.password);
    if (m == null)
    {
      // NOTE: This should return a 404!
      var res = NotFound<LoginResponse>("Invalid username or password!");
      res.Code = LoginController.LOGIN_FAILED;
      return res;
    }
    DAL.RemovePasswordResetData(m.Username);

    // Set the auth token cookie too?
    // NOTE: Here we can interprt options to decide if the user can be logged in, even if they aren't verifed.
    string msg = "OK";
    int code = 0;
    bool isVerified = m.VerifiedOn != null;
    if (!isVerified)
    {
      msg = $"User: {m.Username} is not verified.";
      code = LoginController.NOT_VERFIED;
    }

    // TODO: OPTIONS:
    bool isLoggedIn = true;
    bool ALLOW_UNVERIFIED_LOGIN = false;
    if (!isVerified && !ALLOW_UNVERIFIED_LOGIN)
    {
      isLoggedIn = false;
    }

    m.IsLoggedIn = isLoggedIn;
    m.IP = IPAddress;


    // Now we create our entry for membership.
    if (isLoggedIn)
    {
      m.LoggedInSince = DateTime.UtcNow;
      m.LastActive = m.LoggedInSince;

      string cookieVal = MembershipHelper.CreateLoginCookie();

      // TODO: Invent a proper 'Set/GetCookie' functions on the base class...
      MembershipCookie = cookieVal;

      _LoginToken = MembershipHelper.GetLoginToken(cookieVal, m.IP);
      if (_LoginToken == null)
      {
        throw new InvalidOperationException("Could not generate a login token!");
      }
      MembershipHelper.SetLoggedInUser(m, _LoginToken);
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
    if (IsLoggedIn())
    {
      return new BasicResponse()
      {
        Code = LoginController.LOGGED_IN,
        Message = "You are already logged in"
      };
    }

    // TODO: Some kind of cookie check to make sure that the user was actually directed to reverify!
    // That means a one-time response cookie from the login, and then we should be handing that cookie off to this request...
    // NOTE: That kind of handshake is kind of advanced, and may not be needed....


    // TODO: A logged in user should get a 404 or some other error for this ?
    var member = DAL.GetMember(args.Username);
    if (member != null)
    {
      // NOTE: A user that is already verified shouldn't be here anyway, so we aren't
      // going to indicate that anything is amiss.
      if (!member.IsVerified)
      {
        member = DAL.RefreshVerification(member.Username, MemberManConfig.VerifyWindow);
        SendVerificationMessage(member);
      }
    }

    return new BasicResponse()
    {
      Message = "OK"
    };
  }


  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/verify")]
  public BasicResponse VerifyUser([FromBody] VerificationArgs args)
  {
    if (IsLoggedIn())
    {
      return new BasicResponse()
      {
        Code = LoginController.LOGGED_IN,
        Message = "You are already logged in"
      };
    }

    var res = new BasicResponse()
    {
      Code = 0,
      Message = "OK"
    };

    string code = args.VerificationCode ?? string.Empty;
    Member? member = DAL.GetMemberByVerification(code);
    if (member == null)
    {
      res.Code = INVALID_VERIFICATION;
      res.Message = "Invalid verification code";
    }
    else
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      if (now > member.VerificationExpiration)
      {

        res.Code = VERIFICATION_EXPIRED;
        res.Message = "Verification code is expired.";
        return res;
      }

      DAL.CompleteVerification(member, now);


      // Send out the final email...
      // TEST: Make sure that the email is captured in the test cases.
      SendVerifyCompleteMessage(member);

      res.Code = 0;
      res.Message = "OK";
    }

    return res;
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
    if (IsLoggedIn())
    {
      return new SignupResponse()
      {
        Code = LoginController.LOGGED_IN,
        Message = "You are already logged in"
      };
    }

    // TEST:  How can we test attributes / filters in netcore? (we would have to pass cookies around....)
    // TODO: A logged in user should get a 404 or some other error for this... (is there a 200 level code that can articulate this correctly?  are we getting ballz deep into semantics?)
    // The return code should also indicate that the user is already logged in.....

    ValidateLoginData(login);

    MemberAvailability availability = DAL.CheckAvailability(login.username, login.email);
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

      Member m = DAL.CreateMember(login.username, login.email, login.password, MemberManConfig.VerifyWindow);

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
  protected void ValidateLoginData(LoginModel login)
  {
    login.username = login.email;
    if (!StringTools_Local.IsValidEmail(login.email))
    {
      throw new InvalidOperationException("Invalid email address!");
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SendForgotPasswordMessage(Member member, string resetCode)
  {
    Email email = CreateForgotPasswordEmail(member, resetCode);
    this._Email.SendEmail(email);
  }

  // -------------------------------------------------------------------------------------------------------------------------- 
  private Email CreateForgotPasswordEmail(Member member, string resetCode)
  {
    string resetTime = MemberManConfig.PasswordResetWindow.Humanize(maxUnit: TimeUnit.Hour);

    var model = new
    {
      ResetLink = MemberManConfig.PasswordResetUrl + "?resetToken=" + resetCode,
      ResetTime = resetTime,
    };

    string templateText = GetTemplateText(EmailTemplateNames.FORGOT_PASSWORD_TEMPLATE);
    var t = Template.Parse(templateText);
    string final = t.Render(Hash.FromAnonymousObject(new { model = model }));

    var res = new Email(MemberManConfig.VerificationSender, member.Email, "Reset your Password!", final, true);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SendVerificationMessage(Member member)
  {
    Email email = CreateVerificationEmail(member);
    this._Email.SendEmail(email);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SendVerifyCompleteMessage(Member member)
  {
    Email email = CreateVerifyCompleteEmail(member);
    this._Email.SendEmail(email);
  }


  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual Email CreateVerifyCompleteEmail(Member m)
  {
    string templateText = IOFile.ReadAllText(Path.Combine(FileTools.GetLocalDir("EmailTemplates"), "VerifyComplete.html"));


    var mmCfg = _ConfigHelper.Get<MemberManConfig>();
    string link = mmCfg.VerifyAccountUrl + $"?resetToken={m.VerificationCode}";

    // var date = new DateTimeOffset( m.VerificationExpiration
    // TODO: Localize to EST and include that in the email.
    string expires = m.VerificationExpiration.ToString("MM/dd/yyyy at hh:mm:ss");

    var model = new
    {
      LoginUrl = mmCfg.LoginUrl
    };

    var t = Template.Parse(templateText);
    string final = t.Render(Hash.FromAnonymousObject(new { model = model }));

    var res = new Email(MemberManConfig.VerificationSender, m.Email, "Verify your account!", final, true);
    return res;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual Email CreateVerificationEmail(Member m)
  {
    // TODO: This should be overridable....
    string templateText = GetTemplateText(EmailTemplateNames.VERIFICATION_TEMPLATE);

    var mmCfg = _ConfigHelper.Get<MemberManConfig>();
    string link = mmCfg.VerifyAccountUrl + $"?resetToken={m.VerificationCode}";

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


    var res = new Email(MemberManConfig.VerificationSender, m.Email, "Verify your account!", final, true);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Given the template name, this function will return the template text.
  /// Override the function to use custom templates....
  /// </summary>
  protected virtual string GetTemplateText(string templateName)
  {
    string templateFilePath = Path.Combine(FileTools.GetLocalDir("EmailTemplates"), $"{templateName}.html");
    string res = IOFile.ReadAllText(templateFilePath);
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
public static class Cookies
{
  public const string REVERIFY = "reverifytoken";

}

// ===========================================================================================
public record class ResetPasswordArgs(string ResetToken, string NewPassword, string ConfirmPassword);


// ===========================================================================================
public class ForgotPasswordArgs
{
  public string Username { get; set; } = default!;
}