using Dapper;
using Microsoft.Data.Sqlite;
using officepark.io.Data;

namespace officepark.io.Membership;

// ==========================================================================  
public class SqliteMemberAccess : IMemberAccess
{

  // NOTE: This is very similar, or if not the same as SqliteTimeManAccess.  Maybe
  // we can create a base class or something at some point.
  private string DBFilePath = null;
  private string DataDirectory = null;
  private string ConnectionString;


  // --------------------------------------------------------------------------------------------------------------------------
  public SqliteMemberAccess(string dataDir, string dbFileName)
  {
    DataDirectory = dataDir;
    DBFilePath = Path.Combine(DataDirectory, $"{dbFileName}.sqlite");
    ConnectionString = $"Data Source={DBFilePath};Mode=ReadWriteCreate";

    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
    SqlMapper.AddTypeHandler<DateTimeOffset>(new DateTimeOffsetHandler());
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public Member? CheckLogin(string username, string password)
  {
    throw new NotImplementedException();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member CreateMember(string username, string password)
  {
      throw new NotImplementedException();
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