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
    public string? CurrentUserID { get; private set; }
    public void SetCurrentUser(string? userID)
    {
      _CurrentUserID = userID;
    }
    public string ValidateUser()
    {
      if (string.IsNullOrWhiteSpace(_CurrentUserID)) { throw new InvalidOperationException("The current user ID is null!"); }
      return _CurrentUserID;
    }

    private DataTableFile<TimeManSession> Sessions;

    // private object 
    private ConcurrentDictionary<string, object> UserDataLocks = new ConcurrentDictionary<string, object>();


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManDataAccess(string dataDir)
    {
      DataDirectory = dataDir;
      Sessions = new DataTableFile<TimeManSession>(DataDirectory);
    }

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
    public void SaveSession(TimeManSession session)
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
        SaveSession(sesh);
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
        SaveSession(res);

        Sessions.AddItem(res);
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
      TimeManSession? res = Sessions.GetItem(sessionID);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public IEnumerable<TimeManSession> GetSessions()
    {
      List<TimeManSession> res = Sessions.GetItems();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public IEnumerable<TimeManSession> GetSessions(Predicate<TimeManSession> filter)
    {
      var res = Sessions.GetItems().Where(x => filter(x));
      return res;
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
    /// <summary>
    /// Adds a time mark to the currently active session.
    /// </summary>
    public void AddTimeMark(TimeMark mark)
    {
      TimeManSession? session = GetCurrentSession();
      if (session == null)
      {
        throw new InvalidOperationException("You may not add a time mark if there isn't a currently active session!");
      }

      AddTimeMark(mark, session);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void AddTimeMark(TimeMark mark, TimeManSession session)
    {
      string userID = ValidateUser();
      if (session.HasEnded && mark.Timestamp > session.EndTime)
      {
        throw new InvalidOperationException("You can't add a time mark that exceeds the end time of the session!");
      }

      if (session.UserID != userID)
      {
        throw new InvalidOperationException($"The session user ({session.UserID}) doesn't match the current user ID ({userID})");
      }

      session.TimeMarks.Add(mark);
      SaveSession(session);
    }

  }
}
