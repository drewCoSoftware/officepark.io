using drewCo.Tools;
using Microsoft.AspNetCore.Mvc;
using TimeManServer.Data;
using IOFile = System.IO.File;


namespace TimeManServer.ApiControllers
{
  public class TimeManApiResponse
  {
    public bool IsAuthenticated { get; set; }
  }

  public class SessionResponse : TimeManApiResponse
  {
    public TimeManSession? Session { get; set; }
  }

  public class TimeManApiController : ControllerBase
  {
    // NOTE: We are assuming all sqlite data access for the time being...
    public string DatabaseFileName { get; private set; }
    private ITimeManDataAccess DAL = null!;

    public TimeManApiController()
    {
      DatabaseFileName = Environment.GetEnvironmentVariable("TIMEMAN_DB_FILENAME") ?? "TimeManDB";
      DAL = InitDataAccess();
    }


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

  [ApiController]
  [Route("[controller]")]
  public class SessionsController : TimeManApiController
  {




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
      return res;
    }
  }
}
