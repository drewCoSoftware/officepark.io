using Dapper;
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

    string query = "INSERT INTO Members (username,email,createdon,verifiedon,permissions,password) VALUES (@Username,@Email,@CreatedOn,@VerifiedOn,@Permissions,@Password) RETURNING id";

    IMemberAccess t = this;
    string usePassword = t.GetPasswordHash(password);
    var m = new Member()
    {
      Username = username,
      Password = usePassword,
      Email = email,
      CreatedOn = DateTimeOffset.UtcNow,
      VerifiedOn = DateTime.MinValue,
      Permissions = "BASIC"
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
}

// ==========================================================================
public class MemberManSchema
{
  public List<Member> Members { get; set; } = new List<Member>();
}