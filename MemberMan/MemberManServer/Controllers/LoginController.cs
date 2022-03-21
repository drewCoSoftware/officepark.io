
using Microsoft.AspNetCore.Mvc;
using officepark.io.API;

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
    public string? password { get; set; }
  }

  // ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class LoginController 
{

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
