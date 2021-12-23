using drewCo.Tools;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeManUI.Data
{

  // ============================================================================================================================
  /// <summary>
  /// This is a filesystem based data driver for time man.
  /// </summary>
  public class TimeManDataAccess : ITimeManDataAccess
  {
    public string DataDirectory { get; private set; }


    private string? _CurrentUserID = null;
    public string CurrentUserID { get; private set; }
    public void SetCurrentUser(string userID)
    {
      _CurrentUserID = userID;
    }
    public string ValidateUser()
    {
      if (string.IsNullOrWhiteSpace(_CurrentUserID)) { throw new InvalidOperationException("The current user ID is null!"); }
      return _CurrentUserID;
    }

    private ConcurrentDictionary<string, DataTableFile<TimeManSession>> SessionHistories = new ConcurrentDictionary<string, DataTableFile<TimeManSession>>();

    public TimeManDataAccess(string dataDir)
    {
      DataDirectory = dataDir;
    }

    // private object 
    private ConcurrentDictionary<string, object> UserDataLocks = new ConcurrentDictionary<string, object>();


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? GetCurrentSession()
    {
      string userID = ValidateUser();
      string dataPath = GetCurrentSessionPath(userID);
      if (File.Exists(dataPath))
      {
        var data = File.ReadAllText(dataPath);
        var sesh = JsonSerializer.Deserialize<TimeManSession>(data);

        if (sesh.HasEnded) { return null; }
        return sesh;
      }
      return null;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void SaveCurrentSession(TimeManSession session)
    {
      if (session == null)
      {
        throw new ArgumentNullException(nameof(session));
      }

      // We want to write this data to disk...
      SafeWrite(session.UserID, () =>
      {
        string path = GetCurrentSessionPath(session.UserID);
        string? dir = Path.GetDirectoryName(path);
        FileTools.CreateDirectory(dir);

        string data = JsonSerializer.Serialize(session);
        File.WriteAllText(path, data);
      });
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession StartSession(DateTimeOffset timestamp)
    {
      string userID = ValidateUser();

      TimeManSession? cur = GetCurrentSession();
      if (cur == null || cur.HasEnded)
      {
        TimeManSession sesh = new TimeManSession()
        {
          UserID = userID,
          StartTime = timestamp
        };
        SaveCurrentSession(sesh);
        return sesh;
      }

      if (cur.HasStarted)
      {
        EndSession(timestamp);
        return StartSession(timestamp);
      }

      return cur;
    }


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? EndSession(DateTimeOffset timestamp)
    {
      var res = GetCurrentSession();

      if (res != null && !res.HasEnded)
      {
        res.EndTime = timestamp;
        SaveCurrentSession(res);

        var history = ResolveSessionHistory();
        history.AddItem(res);
      }

      return res;
    }



    // --------------------------------------------------------------------------------------------------------------------------
    private string GetUserDir(string userID)
    {
      string res = Path.Combine(DataDirectory, userID);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private string GetSessionDir(string userID)
    {
      string res = Path.Combine(GetUserDir(userID), "Sessions");
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private string GetCurrentSessionPath(string userID)
    {
      string res = Path.Combine(GetSessionDir(userID), "CurSession.json");
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private string GetSessionHistoryDirectory(string userID)
    {
      string res = Path.Combine(GetSessionDir(userID), "SessionHistory");
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void SafeWrite(string userID, Action action)
    {
      object l = ResolveLock(userID);
      lock (l)
      {
        action();
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private object ResolveLock(string userID)
    {
      if (!UserDataLocks.TryGetValue(userID, out object res))
      {
        res = new object();
        UserDataLocks.TryAdd(userID, res);
      }

      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? GetSession(int sessionID)
    {
      DataTableFile<TimeManSession> history = ResolveSessionHistory();
      TimeManSession? res = history.GetItem(sessionID);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private DataTableFile<TimeManSession> ResolveSessionHistory()
    {
      string userID = ValidateUser();

      if (SessionHistories.TryGetValue(userID, out var res))
      {
        return res;
      }

      int tryCount = 0;
      const int SANITY_COUNT = 100;
      while (!SessionHistories.TryAdd(userID, new DataTableFile<TimeManSession>(GetSessionHistoryDirectory(userID))))
      {
        ++tryCount;
        if (tryCount > SANITY_COUNT)
        {
          throw new InvalidOperationException($"Could not create a new session history for user {userID}!  Max attempts exceeded!");
        }
      }
      return SessionHistories[userID];
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void CancelCurrentSession()
    {
      string userID = ValidateUser();

      // Destroy the session file.
      SafeWrite(userID, () =>
      {
        string path = GetCurrentSessionPath(userID);
        FileTools.DeleteExistingFile(path);
      });
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public List<TimeManSession> GetSessions()
    {
      var history = ResolveSessionHistory();
      List<TimeManSession> res = history.GetItems();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeMark AddTimeMark(TimeManSession session)
    {
      throw new NotImplementedException();
    }


  }
}
