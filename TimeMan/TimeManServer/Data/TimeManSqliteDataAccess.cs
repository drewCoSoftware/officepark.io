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
  public class DateTimeOffsetHandler : TypeHandler<DateTimeOffset>
  {
    // --------------------------------------------------------------------------------------------------------------------------
    public override DateTimeOffset Parse(object value)
    {
      if (value == null) { return DateTimeOffset.MinValue; }
      if (DateTimeOffset.TryParse(value as string, out DateTimeOffset res))
      {
        return res;
      }
      throw new InvalidOperationException($"Input value: '{value as string}' is not a valid DateTimeOffset type!");
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
      parameter.Value = value;
    }
  }


  // ============================================================================================================================
  /// <summary>
  /// This is a filesystem based data driver for time man.
  /// </summary>
  public class TimeManSqliteDataAccess : ITimeManDataAccess
  {

    // This is the ISO8601 format mentioned in:
    // https://www.sqlite.org/datatype3.html
    public const string SQLITE_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fffffff";

    public string DataDirectory { get; private set; }
    private string DBFilePath { get; set; }
    private string ConnectionString;

    private string? _CurrentUserID = null;
    public string? CurrentUserID { get; private set; }

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSqliteDataAccess(string dataDir, string dbFileName)
    {
      DataDirectory = dataDir;
      DBFilePath = Path.Combine(DataDirectory, $"{dbFileName}.sqlite");
      ConnectionString = $"Data Source={DBFilePath};Mode=ReadWriteCreate";

      SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
      SqlMapper.AddTypeHandler<DateTimeOffset>(new DateTimeOffsetHandler());
    }


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
    /// <summary>
    /// This makes sure that we have a database, and the schema is correct.
    /// </summary>
    public void SetupDatabase()
    {
      // Look at the current schema, and make sure that it is up to date....
      bool hasCorrectSchema = ValidateSchema();
      if (!hasCorrectSchema)
      {
        CreateSchema();
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void CreateSchema()
    {
      var schema = new SchemaDefinition(new SqliteFlavor(), typeof(TimeManSchema));

      string query = schema.GetCreateSQL();

      var conn = new SqliteConnection(ConnectionString);
      conn.Open();
      using (var tx = conn.BeginTransaction())
      {
        conn.Execute(query);
        tx.Commit();
      }
      conn.Close();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private bool ValidateSchema()
    {
      // Make sure that the file exists!
      var parts = ConnectionString.Split(";");
      foreach (var p in parts)
      {
        if (p.StartsWith("Data Source"))
        {
          string filePath = p.Split("=")[1].Trim();
          if (!File.Exists(filePath))
          {
            Debug.WriteLine($"The database file at: {filePath} does not exist!");
            return false;
          }
        }
      }
      // NOTE: This is simple.  In the future we could come up with a more robust verison of this.
      bool res = HasTable(nameof(TimeManSchema.Sessions));
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private bool HasTable(string tableName)
    {
      // Helpful:
      // https://www.sqlite.org/schematab.html

      // NOTE: Later we can find a way to validate schema versions or whatever....
      var conn = new SqliteConnection(ConnectionString);
      conn.Open();
      string query = $"select * from sqlite_schema where type = 'table' AND tbl_name=@tableName";

      var qr = conn.Query(query, new { tableName = tableName });
      bool res = qr.Count() > 0;
      conn.Close();

      return res;

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
    /// Runs a database transaction, automatically rolling it back if there is an exception.
    /// </summary>
    private void Transaction(Action<SqliteConnection> txWork)
    {
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();

        using (var tx = conn.BeginTransaction())
        {
          try
          {
            txWork(conn);
            tx.Commit();
          }
          catch (Exception ex)
          {
            // TODO: A better logging mechanism!
            Console.WriteLine($"An exception was encountered when trying to execute the transaction!");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Transaction will be rolled back!");

            tx.Rollback();
          }
        }
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IEnumerable<T> RunQuery<T>(string query, object qParams)
    {
      // NOTE: This connection object could be abstracted more so that we could handle
      // connection pooling, etc. as neeed.
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();
        return RunQuery<T>(conn, query, qParams);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IEnumerable<T> RunQuery<T>(SqliteConnection conn, string query, object parameters)
    {
      var res = conn.Query<T>(query, parameters);
      return res;
    }


    // --------------------------------------------------------------------------------------------------------------------------
    private void RunExecute(string query, object qParams)
    {
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();
        RunExecute(conn, query, qParams);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void RunExecute(SqliteConnection conn, string query, object qParams)
    {
      conn.Execute(query, qParams);
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
