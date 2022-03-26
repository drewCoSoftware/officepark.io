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

    string query = "INSERT INTO Members (username,email,membersince,permissions,password) VALUES (@Username,@Email,@MemberSince,@Permissions,@Password) RETURNING id";

    IMemberAccess t = this;
    string usePassword = t.GetPasswordHash(password);
    var m = new Member()
    {
      Username = username,
      Password = usePassword,
      Email = email,
      MemberSince = DateTimeOffset.UtcNow,
      Permissions = "BASIC"
    };
    int id = RunSingleQuery<int>(query, m);

    m.ID = id;
    return m;
    //      string query = "insert into members (username, email, membersince, permissions, password) VALUES (%) RETURNING id";
    //       var conn = new SqliteConnection(ConnectionString);
    //       conn.Open();
    // //string query = $"insert into "
    //       conn.Close();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void RemoveMember(string username)
  {
      string query = "DELETE FROM members WHERE username = @username";
      RunExecute(query, new { @username = username });
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
}

// ==========================================================================
public class MemberManSchema
{
  public List<Member> Members { get; set; } = new List<Member>();
}