
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
  public string? username { get; set; }
  public string? email { get; set; }
  public string? password { get; set; }
}

// ==========================================================================
public class SignupResponse : BasicResponse
{
  public MemberAvailability Availability { get; set; }
}

// ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class LoginController
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
  public IAPIResponse Signup(LoginModel login)
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
    
    throw new InvalidOperationException();

    Member m = _DAL.CreateMember(login.username, login.email, login.password);

    // This is where we will send out the verification, etc. emails.


    return new BasicResponse()
    {
      AuthRequired = false,
      Message = "Signup OK!"
    };

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
