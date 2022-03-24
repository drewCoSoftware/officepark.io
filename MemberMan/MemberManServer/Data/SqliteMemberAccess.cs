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
  public Member CreateMember(string username, string password)
  { 
      throw new NotImplementedException();
      string query = "insert into members (username, email, membersince, permissions, password) VALUES (%) RETURNING id";
//       var conn = new SqliteConnection(ConnectionString);
//       conn.Open();
// //string query = $"insert into "
//       conn.Close();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetMemberByName(string name)
  {
    throw new NotImplementedException();
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