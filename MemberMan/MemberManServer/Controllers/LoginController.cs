
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
  public bool LoginOK { get; set; }

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

// ==========================================================================
public class SignupResponse : BasicResponse
{
  public MemberAvailability Availability { get; set; }
}


// ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class LoginController : ApiController
{
  public const int INVALID_VERIFICATION = 1;
  public const int VERIFICATION_EXPIRED = 2;

  // --------------------------------------------------------------------------------------------------------------------------
  private IMemberAccess _DAL = null;
  private IEmailService _Email = null;
  public LoginController(IMemberAccess dal_, IEmailService email_)
  {
    _DAL = dal_;
    _Email = email_;
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
    MemberAvailability availability = _DAL.CheckAvailability(login.username, login.email);
    bool isAvailable = availability.IsUsernameAvailable && availability.IsEmailAvailable;
    if (!isAvailable)
    {
      var msgs = new List<string>();
      if (!availability.IsUsernameAvailable)
      {
        msgs.Add($"The username '{login.username}' is in use!");
      }
      if (!availability.IsEmailAvailable)
      {
        msgs.Add($"The email address '{login.email}' is in use!");
      }

      return new SignupResponse()
      {
        Availability = availability,
        Message = string.Join('\n', msgs)
      };
    }

    Member m = _DAL.CreateMember(login.username, login.email, login.password);

    // This is where we will send out the verification, etc. emails.
    SendVerificationMessage(m);

    return new SignupResponse()
    {
      AuthRequired = false,
      Message = "Signup OK!"
    };

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SendVerificationMessage(Member m)
  {
    Email email = CreateVerificationEmail(m);
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
    return new LoginResponse()
    {
      LoginOK = true,
      AuthRequired = true,
      Message = "OK",
      DisplayName = login.username, 
    };
  }



}
