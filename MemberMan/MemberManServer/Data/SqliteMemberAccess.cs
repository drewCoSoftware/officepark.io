using Dapper;
using DataHelpers.Data;
using drewCo.Tools;
using MemberMan;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.Data.Sqlite;
using static MemberMan.LoginController;

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
    // string hash = (this as IMemberAccess).GetPasswordHash(password);

    //// We need to get the stored password first...
    //string storedHash = GetStoredHash(username);
    //noo

    string query = "SELECT * FROM Members WHERE username = @username";
    var res = RunSingleQuery<Member>(query, new
    {
      username = username,
    });

    // Check to see if the password is OK....
    // If not, return null.
    if (res != null)
    {
      bool passOK = (this as IMemberAccess).CheckPassword(password, res.Password);
      if (!passOK)
      {
        return null;
      }
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private string? GetStoredHash(string username)
  {
    string query = "SELECT password FROM members WHERE username = @username";
    string? res = RunSingleQuery<string?>(query, new { username = username });
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member CreateMember(string username, string email, string password, TimeSpan verifyWindow)
  {
    if (!StringTools_Local.IsValidEmail(email))
    {
      throw new InvalidOperationException("Invalid email address!");
    }

    string query = "INSERT INTO Members (username,email,createdon,verificationcode,verificationexpiration,permissions,password) VALUES (@Username,@Email,@CreatedOn,@VerificationCode,@VerificationExpiration,@Permissions,@Password) RETURNING id";

    IMemberAccess t = this;
    string usePassword = t.GetPasswordHash(password);
    var m = new Member()
    {
      Username = username,
      Password = usePassword,
      Email = email,
      CreatedOn = DateTimeOffset.UtcNow,
      Permissions = "BASIC",
    };
    SetVerificationProps(m, verifyWindow);
    int id = RunSingleQuery<int>(query, m);

    m.ID = id;
    return m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SetVerificationProps(Member m, TimeSpan verifyWindow)
  {
    m.VerificationCode = StringTools.ComputeMD5(RandomTools.GetAlphaNumericString(16));
    m.VerificationExpiration = DateTimeOffset.Now + verifyWindow;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void RemoveMember(string username, bool mustExist = true)
  {
    // TODO: We don't actually want to be able to perma-delete users!
    // If anything we should deactivate them, or move their entries to some kind
    // of deactivated table.
    string query = "DELETE FROM members WHERE username = @username";
    int removed = RunExecute(query, new { @username = username });

    // NOTE: This is misleading because a failed removal doesn't necessarily mean that the user didn't exist.  There could be a differnt reason...
    if (removed != 1 && mustExist)
    {
      throw new InvalidOperationException($"Unable to remove the member: {username}");
    }
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
    var updateVerification = "UPDATE members SET verifiedon = @date, verificationexpiration = @verifyExpired, verificationcode = null WHERE username = @username";
    int affected = RunExecute(updateVerification, new
    {
      date,
      username = m.Username,
      verifyExpired = DateTimeOffset.MinValue
    });
    if (affected == 0)
    {
      Console.WriteLine($"Update query for user {m.Username} did not have an effect!");
    }
    //    });
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member RefreshVerification(string username, TimeSpan verifyWindow)
  {
    Member? m = GetMemberByName(username);
    if (m == null)
    {
      throw new InvalidOperationException($"The user with name: {username} does not exist!");
    }
    SetVerificationProps(m, verifyWindow);

    string query = "UPDATE members SET verificationcode = @code, verificationexpiration = @expires WHERE username = @name";
    int updated = RunExecute(query, new
    {
      code = m.VerificationCode,
      expires = m.VerificationExpiration,
      name = m.Username
    });
    if (updated != 1)
    {
      throw new InvalidOperationException("Verification data could not be refreshed!");
    }

    return m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal string SetPermissions(string username, string permissions)
  {
    string query = "UPDATE members SET permissions = @permissions WHERE username = @username";
    int updated = RunExecute(query, new
    {
      permissions = permissions,
      username = username
    });

    if (updated != 1)
    {
      throw new InvalidOperationException($"Unable to set permissions for member: {username}!");
    }

    return permissions;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal void UpdateMember(Member m)
  {
    var def = this.SchemaDef.GetTableDef(typeof(Member)); // nameof(Member))!;
    string query = def.GetUpdateQuery();

    int updated = RunExecute(query, m);
    if (updated != 1)
    {
      throw new InvalidOperationException($"Unable to update data for member: {m.Username}!");
    }
  }
}

// ==========================================================================
public class MemberManSchema
{
  public List<Member> Members { get; set; } = new List<Member>();
}