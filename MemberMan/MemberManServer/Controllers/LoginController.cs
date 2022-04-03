
using System.Text;
using Microsoft.AspNetCore.Mvc;
using officepark.io.API;
using officepark.io.Membership;

namespace MemberMan;

// ============================================================================================================================
public class LoginResponse : BasicResponse
{
  public bool LoginOK { get; set; }
}

// ============================================================================================================================
public class LoginModel
{
  public string username { get; set; } = string.Empty;
  public string email { get; set; } = string.Empty;
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
public class LoginController : ControllerBase
{
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
    Email email = CreateVerificationEmail(m);
    this._Email.SendEmail(email);


    return new SignupResponse()
    {
      AuthRequired = false,
      Message = "Signup OK!"
    };

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
  public IAPIResponse VerifyUser(string code)
  {
    var res = new BasicResponse()
    {
      ResponseCode = 0,
      Message = "OK"
    };

    Member? m = _DAL.GetMemberByVerification(code);
    if (m == null)
    {
      res.ResponseCode = 1;
      res.Message = "Invalid verification code";
    }
    else
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      if (m.VerificationExpiration == null || now > m.VerificationExpiration)
      {
        res.ResponseCode = 2;
        res.Message = "Verification is expired";
      }

      _DAL.CompleteVerification(m, now);
      res.ResponseCode = 0;
      res.Message = "OK";
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpPost]
  [Route("/api/login")]
  public IAPIResponse Login(LoginModel login)
  {
    return new LoginResponse()
    {
      LoginOK = false,
      AuthRequired = false,
      Message = "This is not fully implemented!"
    };
    // We need to take the input creds, and bounce them against our internal database/filestore/whatever.
    // That means that we need some way to configure those services.....
    // var login = new LoginModel()
    // {
    //     username = "abc",
    //     password = "def123"
    // };
    // return new { username = login.username, password = login.password, message = "gravy!" };
  }



}
