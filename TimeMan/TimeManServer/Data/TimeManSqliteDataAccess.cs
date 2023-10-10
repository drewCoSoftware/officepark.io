using drewCo.Tools;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using static Dapper.SqlMapper;
using System.Data;
using System.Diagnostics;

using officepark.io.Data;

namespace TimeManServer.Data;

// ============================================================================================================================
/// <summary>
/// This is a filesystem based data driver for time man.
/// </summary>
public class TimeManSqliteDataAccess : SqliteDataAccess<TimeManSchema>, ITimeManDataAccess
{

  private string? _CurrentUserID = null;
  public string? CurrentUserID { get; private set; }

  // --------------------------------------------------------------------------------------------------------------------------
  public TimeManSqliteDataAccess(string dataDir, string dbFileName)
    : base(dataDir, dbFileName)
  { }

  // --------------------------------------------------------------------------------------------------------------------------
  public void SetCurrentUser(string? userID)
  {
    _CurrentUserID = userID;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public string ValidateUser()
  {
    if (string.IsNullOrWhiteSpace(_CurrentUserID)) { throw new InvalidOperationException("The current user ID is null!"); }
    return _CurrentUserID;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public TimeManSession? GetCurrentSession()
  {
    string userID = ValidateUser();

    // NOTE: These queries will have to be sensitive to the names that are generated during schema creation.
    string query = $"SELECT * from Sessions where UserID = @userID AND EndTime IS NULL";

    TimeManSession? res = RunQuery<TimeManSession>(query, new { userID = userID }).FirstOrDefault();

    if (res == null || res.HasEnded)
    {
      res = null;
    }

    return res;
  }



  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Save or update the given session data.
  /// </summary>
  public void SaveSession(TimeManSession session)
  {
    if (session == null)
    {
      throw new ArgumentNullException(nameof(session));
    }
    var qParams = new
    {
      userID = session.UserID,
      startTime = session.StartTime?.ToString(SQLITE_DATETIME_FORMAT),
      endTime = session.EndTime?.ToString(SQLITE_DATETIME_FORMAT),
      sessionID = session.ID
    };

    if (session.ID == 0)
    {
      string query = "INSERT INTO Sessions (UserID, StartTime, EndTime) VALUES (@userID, @startTime, @endTime) RETURNING ID";
      int newID = RunQuery<int>(query, qParams).First();
      session.ID = newID;
    }
    else
    {
      // We are updating exiting data.
      string query = "UPDATE Sessions SET StartTime = @startTime, EndTime = @endTime WHERE ID = @sessionID";
      RunExecute(query, qParams);
    }

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
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public TimeManSession? GetSession(int sessionID)
  {
    // NOTE: Some way to get all of the time-marks would be good too....
    string query = "SELECT * FROM Sessions WHERE ID = @id";
    var qParams = new { id = sessionID };

    var res = RunQuery<TimeManSession>(query, qParams).SingleOrDefault();
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public IEnumerable<TimeManSession> GetSessions()
  {
    // NOTE: Some way to get all of the time-marks would be good too....
    string query = "SELECT * FROM Sessions";

    var res = RunQuery<TimeManSession>(query, null);
    return res;
  }

  //--------------------------------------------------------------------------------------------------------------------------
  //NOTE: These types of predicate filters won't work with dapper since we have to
  //create the queries by hand.Some kind of automation for this may be in order in the future....
  public IEnumerable<TimeManSession> GetSessions(string userID)
  {
    string query = "SELECT * FROM Sessions WHERE UserID = @userID";
    var qParams = new { userID = userID };

    var res = RunQuery<TimeManSession>(query, qParams);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void CancelCurrentSession()
  {
    var current = GetCurrentSession();
    if (current != null)
    {
      Transaction((conn) =>
      {
        var qParams = new { id = current.ID };

        // Cleanup any of the time marks!
        {
          string query = $"DELETE FROM TimeMarks WHERE Sessions_ID = @id";
          RunExecute(conn, query, qParams);
        }

        // Now delete the parent object.
        {
          string query = $"DELETE FROM Sessions WHERE ID = @id";
          RunExecute(conn, query, qParams);
        }
      });

    }
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
