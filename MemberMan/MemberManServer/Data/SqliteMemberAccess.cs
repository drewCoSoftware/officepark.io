using Dapper;
using drewCo.Tools;
using Microsoft.Data.Sqlite;
using officepark.io.Data;

namespace officepark.io.Membership;

// ==========================================================================  
public class SqliteMemberAccess : SqliteDataAccess<MemberManSchema>, IMemberAccess
{



  // --------------------------------------------------------------------------------------------------------------------------
  public SqliteMemberAccess(string dataDir, string dbFileName)
    : base(dataDir, dbFileName)
  { }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? CheckLogin(string username, string password)
  {
    throw new NotImplementedException();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member CreateMember(string username, string email, string password)
  {

    string query = "INSERT INTO Members (username,email,createdon,verificationcode,permissions,password) VALUES (@Username,@Email,@CreatedOn,@VerificationCode,@Permissions,@Password) RETURNING id";

    IMemberAccess t = this;
    string usePassword = t.GetPasswordHash(password);
    var m = new Member()
    {
      Username = username,
      Password = usePassword,
      Email = email,
      CreatedOn = DateTimeOffset.UtcNow,
      Permissions = "BASIC",
      VerificationCode = StringTools.ComputeMD5(RandomTools.GetAlphaNumericString(16))
    };
    int id = RunSingleQuery<int>(query, m);

    m.ID = id;
    return m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void RemoveMember(string username)
  {
    string query = "DELETE FROM members WHERE username = @username";
    int removed = RunExecute(query, new { @username = username });
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetMemberByName(string username)
  {
    Member? res = RunSingleQuery<Member>("SELECT * FROM members WHERE username = @username", new { username = username });
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public List<Member> GetMemberList()
  {
    throw new NotImplementedException();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public MemberAvailability CheckAvailability(string username, string email)
  {
    var byName = "SELECT username FROM members WHERE username = @username";
    var byNameRes = RunQuery<Member>(byName, new { username = username });

    var byEmail = "SELECT email FROM members WHERE email = @email";
    var byEmailRes = RunQuery<Member>(byEmail, new { email = email });

    var res = new MemberAvailability(byNameRes.Count() == 0, byEmailRes.Count() == 0);
    return res;

  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetMemberByVerification(string code)
  {
    var byVerification = "SELECT username, verificationexpiration FROM members WHERE verificationcode = @verificationcode";
    var byVerify = RunSingleQuery<Member>(byVerification, new { @verificationcode = code });
    return byVerify;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void CompleteVerification(Member m, DateTimeOffset date)
  {
    // NOTE: Using the transaction here will lock the database.
    // This is because not passing 'conn' as the first argument to the query function will
    // try to open a new connection, etc.  Obviously we need some way to determine if a connection
    // is already open and use it.  Maybe we can do something with the thread id / locks to better automate
    // this, or at least indicate to the user in debug mode that something is off.
    // Transaction((conn) =>
    // {
      var updateVerification = "UPDATE members SET verifiedon = @date, verificationexpiration = null, verificationcode = null WHERE username = @username";
      int affected = RunExecute(updateVerification, new { date = date, username = m.Username });
      if (affected == 0)
      {
        Console.WriteLine($"Update query for user {m.Username} did not have an effect!");
      }
//    });
  }

}

// ==========================================================================
public class MemberManSchema
{
  public List<Member> Members { get; set; } = new List<Member>();
}