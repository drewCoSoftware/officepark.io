
using System.Runtime.CompilerServices;
using System.Text;
using DotLiquid;
using drewCo.Fetchy;
using drewCo.Tools;
using Humanizer;
using Humanizer.Localisation;
using MailKit.Search;
using MemberManServer;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Net.Http.Headers;
using officepark.io;

using officepark.io.Membership;
using static MemberManServer.Mailer;
using IOFile = System.IO.File;
using drewCo.Fetchy;

namespace MemberMan;

// ============================================================================================================================
public interface IHasMembershipService
{
  MemberManService MMService { get; }
}

// ============================================================================================================================
/// <summary>
/// Indicates that a controller has certain member man features.
/// </summary>
public interface IMemberManFeatures : IHasMembershipService
{
  //  IMemberAccess DAL { get; }
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
public class LoginApiController : ApiController, IHasMembershipService //, IMemberManFeatures
{

  public IMemberAccess DAL { get; private set; } = default!;
  public MemberManConfig MemberManConfig { get; private set; } = null!;
  public MembershipHelper MemberService { get; private set; } = null!;
  private IEmailService _Email = default!;

  public MemberManService MMService { get; private set; } = null!;


  // --------------------------------------------------------------------------------------------------------------------------
  public LoginApiController(MemberManService mmService_, IMemberAccess dal_, IEmailService email_, MembershipHelper mmHelper_)
  {
    if (dal_ == null) { throw new ArgumentNullException("dal_"); }
    if (email_ == null) { throw new ArgumentNullException("email_"); }

    MMService = mmService_;

    DAL = dal_;
    _Email = email_;

    MemberService = mmHelper_;
    MemberManConfig = MemberService.Config;
  }


  #region Properties 


  protected string? _LoginToken = default!;
  public virtual string? LoginToken
  {
    get
    {
      return _LoginToken ?? (_LoginToken = MembershipHelper.GetLoginToken(Request));
    }
    internal set { _LoginToken = value; }
  }

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

  //// --------------------------------------------------------------------------------------------------------------------------
  //public IMemberAccess GetDAL()
  //{
  //  return MMService.GetDAL();
  //}

  public MemberManConfig GetConfig() { return MemberManConfig; }


  // --------------------------------------------------------------------------------------------------------------------------
  // NOTE: This call will always work, we don't need to know if someone is logged in or not....
  [HttpPost]
  [Route("/api/logout")]
  public IFetchyResponse Logout()
  {
    MemberService.Logout(Request, Response);
    _LoginToken = null;

    var res = OK<MemberManBasicResponse>();
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
  public IFetchyResponse ForgotPassword([FromBody] ForgotPasswordArgs args)
  {
    // TODO: Check for and reject if the user is currently logged in.
    if (IsLoggedIn())
    {
      // TODO: Replace with basic response!
      return NotFound();
      //return new BasicResponse()
      //{
      //  Code = MemberManService.ServiceCodes.LOGGED_IN
      //};
    }

    // Find the user.
    var member = MMService.GetMemberByName(args.Username);
    if (member == null)
    {
      return NotFound();
    }

    string resetToken = MMService.BeginPasswordReset(args.Username);

    // With that token, we will generate and send out an email with the reset instructions.
    // Then the email is sent!
    SendForgotPasswordMessage(member, resetToken);

    // NOTE: We always return OK for this function.
    return OK();
  }


  // --------------------------------------------------------------------------------------------------------------------------
  // TODO: This could also go with some kind of base class?
  protected bool IsLoggedIn()
  {
    bool res = MMService.IsLoggedIn(MembershipCookie, IPAddress);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected bool IsLoggedIn(out Member? m)
  {
    m = MMService.GetLoggedInMember(MembershipCookie, IPAddress);
    return m != null;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/reset-password")]
  public IFetchyResponse ResetPassword([FromBody] ResetPasswordArgs args)
  {
    // TODO: Disallow logged in users.
    if (IsLoggedIn())
    {
      return NotFound();
    }

    ResetPasswordResponse response = MMService.ResetPassword(args);
    if (response.Code != 0)
    {    // TODO: Use const for 'OK' code!
      return Error(response.Code, response.Message);
    }
    else
    {
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
    // TEMP:
    Thread.Sleep(2000);

    // If the user is currently logged in, then we can just return OK or something....
    // TODO: Can we make this block of code part of the 'CheckMembership' attribute?
    // TODO: This is also something that should use the properties for LoginToken, IPAddress, etc.
    if (IsLoggedIn())
    {
      var res = OK<LoginResponse>();
      res.IsLoggedIn = true;
      res.Code = MemberManService.ServiceCodes.LOGGED_IN;
      return res;
    }

    Member? m = MMService.Login(login);
    if (m == null)
    {
      // NOTE: This should return a 404!
      var res = NotFound<LoginResponse>("Invalid username or password!");
      res.Code = MemberManService.ServiceCodes.LOGIN_FAILED;
      return res;
    }

    // Set the auth token cookie too?
    // NOTE: Here we can interprt options to decide if the user can be logged in, even if they aren't verifed.
    string msg = "OK";
    int code = 0;
    bool isVerified = m.VerifiedOn != null;
    if (!isVerified)
    {
      msg = $"User: {m.Username} is not verified.";
      code = MemberManService.ServiceCodes.NOT_VERFIED;
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
      MemberService.SetLoggedInUser(m, _LoginToken);
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
  public IFetchyResponse RequestVerification([FromBody] VerificationArgs args)
  {
    if (IsLoggedIn())
    {
      return new MemberManBasicResponse()
      {
        Code = MemberManService.ServiceCodes.LOGGED_IN,
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

    return new MemberManBasicResponse()
    {
      Message = "OK"
    };
  }


  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/verify")]
  public MemberManBasicResponse VerifyUser([FromBody] VerificationArgs args)
  {
    if (IsLoggedIn())
    {
      return new MemberManBasicResponse()
      {
        Code = MemberManService.ServiceCodes.LOGGED_IN,
        Message = "You are already logged in"
      };
    }

    MemberManBasicResponse? testResponse = HandleVerifyTest();
    if (testResponse != null)
    {
      return testResponse;
    }

    string code = args.VerificationCode ?? string.Empty;
    Member? member = DAL.GetMemberByVerification(code);
    if (member == null)
    {
      return InvalidVerificationCode();
    }
    else
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      if (now > member.VerificationExpiration)
      {
        return VerfiyExpired();
      }

      DAL.CompleteVerification(member, now);


      // Send out the final email...
      // TEST: Make sure that the email is captured in the test cases.
      SendVerifyCompleteMessage(member);

      return OK();
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static MemberManBasicResponse InvalidVerificationCode()
  {
    return new MemberManBasicResponse()
    {
      Code = MemberManService.ServiceCodes.INVALID_VERIFICATION,
      Message = "Invalid verification code",
    };
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static MemberManBasicResponse VerfiyExpired()
  {
    return new MemberManBasicResponse()
    {
      Code = MemberManService.ServiceCodes.VERIFICATION_EXPIRED,
      Message = "Verification code is expired.",
    };
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private MemberManBasicResponse? HandleVerifyTest()
  {
    // In test scenarios we don't actually create the user account.
    // NOTE: 'Request' is null when we are running unit tests.  There may be a better way to wrap the
    // code that gets the headers so that we can test them too.
    if (HasHeader(Headers.MM_TEST_MODE))
    {
      // The service pretends that everything is OK, so we don't actually do anything in test mode....
      string testType = GetTestType(Request);
      if (testType != Headers.TestTypes.TEST_PASS)
      {
        throw new NotImplementedException("Invalid test type!");
      }
      else
      {
        // Everything is fine!
        return OK();
      }
    }

    return null;
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
        Code = MemberManService.ServiceCodes.LOGGED_IN,
        Message = "You are already logged in"
      };
    }

    SignupResponse? testResponse = HandleSignupTest();
    if (testResponse != null)
    {
      return testResponse;
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


    Member m = DAL.CreateMember(login.username, login.email, login.password, MemberManConfig.VerifyWindow);

    // This is where we will send out the verification, etc. emails.
    SendVerificationMessage(m);

    return SignupOK();

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private SignupResponse? HandleSignupTest()
  {
    // In test scenarios we don't actually create the user account.
    // NOTE: 'Request' is null when we are running unit tests.  There may be a better way to wrap the
    // code that gets the headers so that we can test them too.
    if (HasHeader(Headers.MM_TEST_MODE))
    {
      // VERBOSE:
      Console.WriteLine($"The test header: {Headers.MM_TEST_MODE} is set!  Normal login operations will be bypassed!");

      // The service pretends that everything is OK, so we don't actually do anything in test mode....
      string testType = GetTestType(Request);
      if (testType != Headers.TestTypes.TEST_PASS)
      {
        throw new NotImplementedException("Invalid test type!");
      }
      else
      {
        return SignupOK();
      }
    }

    return null;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static SignupResponse SignupOK()
  {
    return new SignupResponse()
    {
      IsUsernameAvailable = true,
      AuthRequired = false,
      Message = "Signup OK!",
    };
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private string GetTestType(HttpRequest request)
  {
    // TODO: Wrap this header handling code (get + check) into the base class...
    string res = Headers.TestTypes.TEST_PASS;
    var vals = request.Headers[Headers.MM_TEST_TYPE];
    if (vals.Count > 0)
    {
      res = vals[0];
    }
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected void ValidateLoginData(LoginModel login)
  {
    login.username = login.email;
    if (!StringTools.IsValidEmail(login.email))
    {
      throw new InvalidOperationException("Invalid email address!");
    }

    PasswordValidationResult result = DAL.PasswordValidator.Validate(login.password);
    if (!result.IsValid)
    {
      throw new InvalidOperationException(result.Message);
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

    var res = new Email(MemberManConfig.EmailConfig.Username, member.Email, "Reset your Password!", final, true);
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


    // var date = new DateTimeOffset( m.VerificationExpiration
    // TODO: Localize to EST and include that in the email.
    var model = new
    {
      LoginUrl = MemberManConfig.LoginUrl
    };

    var t = Template.Parse(templateText);
    string final = t.Render(Hash.FromAnonymousObject(new { model = model }));

    var res = new Email(MemberManConfig.EmailConfig.Username, m.Email, "Verify your account!", final, true);
    return res;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual Email CreateVerificationEmail(Member m)
  {
    // TODO: This should be overridable....
    string templateText = GetTemplateText(EmailTemplateNames.VERIFICATION_TEMPLATE);

    var mmCfg = MemberManConfig;
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

    var res = new Email(MemberManConfig.EmailConfig.Username, m.Email, "Verify your account!", final, true);
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

// ===========================================================================================
public record class ResetPasswordArgs(string ResetToken, string NewPassword, string ConfirmPassword);

// ===========================================================================================
public class ForgotPasswordArgs
{
  public string Username { get; set; } = default!;
}