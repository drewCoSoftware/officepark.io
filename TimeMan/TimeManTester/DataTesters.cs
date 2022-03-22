using drewCo.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeManServer.Data;
using Xunit;
using officepark.io.Data;

namespace TimeManTester
{

  // ============================================================================================================================
  public class DataTesters
  {

    // --------------------------------------------------------------------------------------------------------------------------
    public DataTesters()
    {
      FileTools.CreateDirectory(Path.Combine(FileTools.GetAppDir(), "TimeManData"));
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Shows that our schema generator can detect and properly model an implicit one-to-many relationship.
    /// </summary>
    [Fact]
    public void CanDetectOneToManyRelationship()
    {

      var schema = new SchemaDefinition(new SqliteFlavor());
      schema.AddTable<TimeManSession>("Sessions");

      TableDef? sessDef = schema.GetTableDef("Sessions");
      Assert.NotNull(sessDef);

      TableDef? timeMarkDef = schema.GetTableDef("TimeMarks");
      Assert.NotNull(timeMarkDef);

      // The time mark table should have the fk relation to the Sessions table.
      // That is because the dtype 'TimeManSession' has a member that points to a list of time marks.
      var deps = timeMarkDef.DependentTables;
      Assert.Single(deps);
      Assert.Equal("Sessions", deps[0].Def.Name);

    }


    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CantAddTimeMarkWithNoActiveSession()
    {
      const string TEST_USER = nameof(DataAccessDoesntAllowInvalidUserID);
      ITimeManDataAccess da = GetDataAccess(TEST_USER);

      TimeMark mark = new TimeMark()
      {
        Timestamp = DateTimeOffset.Now,
        Category = new WorkCategory()
        {
          Name = "Work"
        }
      };

      Assert.Throws<InvalidOperationException>(() =>
      {
        da.AddTimeMark(mark);
      });

    }

    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void DataAccessDoesntAllowInvalidUserID()
    {
      const string TEST_USER = nameof(DataAccessDoesntAllowInvalidUserID);
      ITimeManDataAccess da = GetDataAccess(TEST_USER);
      da.SetCurrentUser(null);

      Assert.Throws<InvalidOperationException>(() =>
      {
        da.ValidateUser();
      });
    }


    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CanCancelCurrentSession()
    {
      const string TEST_USER = nameof(CanCancelCurrentSession);
      ITimeManDataAccess da = GetDataAccess(TEST_USER);

      TimeManSession? sesh = da.StartSession(DateTimeOffset.Now);
      Assert.NotNull(sesh);

      // There should no longer be a current session for the user.
      da.CancelCurrentSession();
      sesh = da.GetCurrentSession();
      Assert.Null(sesh);


      // Make sure that there are no sessions to be had in the manager.
      List<TimeManSession> sessions = da.GetSessions(TEST_USER).ToList();
      Assert.Empty(sessions);

    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Show that ending a session will write it to the session log.
    /// </summary>
    [Fact]
    public void EndingCurrentSessionSavesItToHistory()
    {

      const string TEST_USER = nameof(EndingCurrentSessionSavesItToHistory);
      ITimeManDataAccess da = GetDataAccess(TEST_USER);

      TimeManSession? sesh = da.StartSession(DateTimeOffset.Now);
      TimeManSession? ended = da.EndSession(DateTimeOffset.Now)!;

      // This should have written the data to the history log.
      Assert.NotNull(ended);
      Assert.True(ended.ID > 0, "The session should have a valid ID after it is saved!");


      // Now we can check the DAL to make sure that our session has been saved in the system!
      TimeManSession? saved = da.GetSession(ended.ID)!;
      Assert.Equal(ended.StartTime, saved.StartTime);
      Assert.Equal(ended.EndTime, saved.EndTime);


      // Make sure that we don't have a current session since we just ended one.
      TimeManSession? current = da.GetCurrentSession();
      Assert.Null(current);
    }



    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CanStartTimeManSession()
    {
      const string TEST_USER = nameof(CanStartTimeManSession);

      ITimeManDataAccess da = GetDataAccess(TEST_USER);

      // The current session should be null, since we don't have one yet.
      TimeManSession? cur = da.GetCurrentSession();
      Assert.Null(cur);


      var newSesh = da.StartSession(DateTimeOffset.Now);
      Assert.NotNull(newSesh);
      Assert.True(newSesh.HasStarted);
      Assert.True(newSesh.ID > 0);

      // Make sure that the new session and current session are the same.
      var curSesh = da.GetCurrentSession()!;
      Assert.NotNull(curSesh);
      Assert.Equal(newSesh.StartTime, curSesh.StartTime);
      Assert.False(newSesh.HasEnded);
      Assert.False(curSesh.HasEnded);
      Assert.Equal(curSesh.ID, newSesh.ID);
    }



    // --------------------------------------------------------------------------------------------------------------------------
    // NOTE: The idea with this function is that we can hook our test cases up to a DB driver, etc.
    // and still get a full set of passing test cases.
    private ITimeManDataAccess GetDataAccess(string userID)
    {
      var res = new TimeManSqliteDataAccess(Path.Combine(FileTools.GetAppDir(), "TimeManData"), "TimeManTesters");
      res.SetupDatabase();

      res.SetCurrentUser(userID);
      res.CancelCurrentSession();

      return res;
    }

  }
}
