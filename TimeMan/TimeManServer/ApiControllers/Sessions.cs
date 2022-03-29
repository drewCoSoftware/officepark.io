// using System.Diagnostics;
// using drewCo.Tools;
// using Microsoft.AspNetCore.Mvc;
// using officepark.io.API;
// using TimeManServer.Data;
// using IOFile = System.IO.File;


using drewCo.Tools;
using Microsoft.AspNetCore.Mvc;
using officepark.io.API;
using TimeManServer.Data;
using IOFile = System.IO.File;

namespace TimeManServer.ApiControllers;

// ==========================================================================
public class TimeManApiResponse
{
  public bool IsAuthenticated { get; set; }
}

// ==========================================================================
public class SessionResponse : TimeManApiResponse
{
  public TimeManSession? Session { get; set; }
}

// ==========================================================================
public class TimeManApiController : ControllerBase
{
  // NOTE: We are assuming all sqlite data access for the time being...
  public string DatabaseFileName { get; private set; }
  private ITimeManDataAccess DAL = null!;

  // --------------------------------------------------------------------------------------------------------------------------
  public TimeManApiController()
  {
    DatabaseFileName = Environment.GetEnvironmentVariable("TIMEMAN_DB_FILENAME") ?? "TimeManDB";
    DAL = InitDataAccess();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private ITimeManDataAccess InitDataAccess()
  {
    // Check for the database file!
    string dataDir = Path.Combine(FileTools.GetAppDir(), "Data");
    string dbPath = Path.Combine(dataDir, DatabaseFileName + ".sqlite");

    var res = new TimeManSqliteDataAccess(dataDir, DatabaseFileName);

    if (!IOFile.Exists(dbPath))
    {
      FileTools.CreateDirectory(dataDir);
      res.SetupDatabase();
    }

    return res;
  }

}

// // ============================================================================================================================
// /// <summary>
// /// This interface type should be implemented by all return types in the system.
// /// The goal is to have common data to program against for messages, errors, auth status, etc.
// /// </summary>
// public interface IAPIResponse
// {
//   /// <summary>
//   /// The user's current auth token.  If null, then the user is not currently
//   /// authorized and should login again.
//   /// </summary>
//   string? AuthToken { get; set; }

//   /// <summary>
//   /// Indicates if the request in question requires authorization.
//   /// </summary>
//   bool AuthRequired { get; set; }

//   /// <summary>
//   /// Any old message that we want to send along.
//   /// </summary>
//   /// <value></value>
//   string? Message { get; set; }
// }

// // ============================================================================================================================
// public class BasicResponse : IAPIResponse
// {
//   public string? AuthToken { get; set; }
//   public bool AuthRequired { get; set; } = true;
//   public string? Message { get; set; }
// }

// // ============================================================================================================================
// public class LoginResponse : BasicResponse
// {
//   public bool LoginOK { get; set; }
// }


// // ============================================================================================================================
// public class LoginModel
// {
//   public string username { get; set; }
//   public string password { get; set; }

//   // public LoginModel() { }
// }


// ============================================================================================================================
[ApiController]
[Route("[controller]")]
public class SessionsController : TimeManApiController
{

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpGet]
  [Route("/api/pingtest")]
  public IAPIResponse PingTest()
  {
    string cookieval = Request.Cookies["cookie-3"] ?? "<null>";
    return new BasicResponse()
    {
      AuthToken = null,
      AuthRequired = false,
      Message = "OK"
    };
  }


  //     // -------------------------------------------------------------------------------------------------------------------------- 
  //     [HttpPost]
  //     [Route("/api/login")]
  // //    public LoginResponse Login([FromForm] string username, [FromForm] string password)
  //     public LoginResponse Login(LoginModel data)
  //     {
  //       // Simulate a long time....
  //       Console.WriteLine("password is: " + data.password);
  //       Console.WriteLine("username is: " + data.username);

  //       // This is where the creds are checked...
  //       LoginResponse res = new LoginResponse()
  //       {
  //         LoginOK = false,
  //       };
  //       return res;
  //     }

  // // -------------------------------------------------------------------------------------------------------------------------- 
  // [HttpGet]
  // [Route("/api/Logout")]
  // public BasicResponse Logout()
  // {
  //   // Check cookies.
  //   // Check IP.
  //   // Log the user out, if they are still in.
  //   return new BasicResponse()
  //   {
  //     Message = "OK"
  //   };
  // }

  // -------------------------------------------------------------------------------------------------------------------------- 
  /// <summary>
  /// Begin a new session!
  /// </summary>
  [HttpPost]
  [Route("/api/[controller]/begin")]
  public object Begin()
  {
    return null;
  }

  // -------------------------------------------------------------------------------------------------------------------------- 
  /// <summary>
  /// End the currently active session, or do nothing if no session is active.
  /// </summary>
  /// <returns></returns>
  [HttpPost]
  [Route("/api/[controller]/end")]
  public object End()
  {
    return null;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [HttpGet]
  [Route("/api/[controller]/active")]
  public object Active()
  {
    // NOTE: We will need to bounce the authentication off of the authentication service....
    //string? userID = Request.Cookies["userID"];
    //if (userID == null)
    //{
    //    return new UnauthorizedResult();
    //}


    var res = new List<TimeManSession>()
            {
                new TimeManSession()
                {
                    UserID = "abc",
                    StartTime = DateTimeOffset.Now
                },
                new TimeManSession()
                {
                    UserID = "123",
                    StartTime = DateTimeOffset.Now + TimeSpan.FromDays(1)
                },
            };

    //   Response.Headers.Add("Access-Control-Allow-Origin", "*");

    return res;
  }
}

