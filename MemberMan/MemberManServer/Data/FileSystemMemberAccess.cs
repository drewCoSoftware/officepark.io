using System.Diagnostics;
using System.Text.Json;
using drewCo.Tools;
using officepark.io.Membership;



// ==========================================================================
/// <summary>
/// This is how we talk to the members in the database, or otherwise!
/// </summary>
public class FileSystemMemberAccess : IMemberAccess
{

  // Cache!  This is something that would be invalidated when a new version of the data file was saved?
  // We should also have some kind of 'cache provider' so that we can manually invalidate data as needed.
  private List<Member> _CachedMembers = new List<Member>();

  private object FSLock = new object();

  // --------------------------------------------------------------------------------------------------------------------------
  public Member CreateMember(string username, string email, string password, TimeSpan verifyWindow)
  {
    // Check for existing user....
    // Explode if one exists...
    Member? check = GetMemberByName(username);
    if (check != null)
    {
      throw new Exception($"A member named: {username} already exists!");
    }

    IMemberAccess t = this;
    Member res = new Member()
    {
      Username = username,
      Password = t.GetPasswordHash(password),
      CreatedOn = DateTimeOffset.UtcNow,
      VerifiedOn = DateTimeOffset.MinValue,
    };

    // Let's save it to disk!
    lock (FSLock)
    {
      var members = GetMemberList();
      members.Add(res);

      SaveMemberList(members);
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------  
  public void RemoveMember(string username, bool mustExist = true)
  {
    throw new NotImplementedException();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SaveMemberList(List<Member> members)
  {
    lock (FSLock)
    {
      _CachedMembers = members;

      string path = GetSavePath(true);
      string data = JsonSerializer.Serialize(members, new JsonSerializerOptions()
      {
        WriteIndented = true
      });

      File.WriteAllText(path, data);
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Given the username and password, returns a member, or null if the username or password is incorrect.
  /// /// </summary>
  public Member? GetMember(string username, string plaintextPassword)
  {
    Member? m = GetMemberByName(username);
    if (m == null)
    {
      // Console.WriteLine($"Could not find user: {username}!");
      return null;
    }

    IMemberAccess t = this;
    if (t.VerifyPassword(plaintextPassword, m.Password))
    {
      return m;
    }

    return null;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// /// Get member ID
  /// </summary>
  public Member? GetMemberByName(string name)
  {
    // NOTE: In a perfect world, this would be setup so that we could use a DB, FileSystem, etc.

    List<Member> members = GetMemberList();
    Member? member = (from x in members
                      where x.Username == name
                      select x)
                     .SingleOrDefault();

    return member;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private string GetSavePath(bool createDir = false)
  {
    string dir = Path.Combine(FileTools.GetAppDir(), "data", "members");
    string path = Path.Combine(dir, "members.json");

    if (createDir) { FileTools.CreateDirectory(dir); }
    return path;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public List<Member> GetMemberList()
  {
    if (_CachedMembers != null) { return _CachedMembers; }

    // string dir = Path.Combine(FileTools.GetAppDir(), "data", "members");
    // string path = Path.Combine(dir, "members.json");
    string path = GetSavePath();

    if (File.Exists(path))
    {
      try
      {
        lock (FSLock)
        {
          string data = File.ReadAllText(path);
          _CachedMembers = JsonSerializer.Deserialize<List<Member>>(data) ?? new List<Member>();
          return _CachedMembers;
        }
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException("Could not read member list from disk!", ex);
      }
    }
    else
    {
      Debug.WriteLine($"The membership file at path: {path} does not exist!");
      return new List<Member>();
    }
  }

  public MemberAvailability CheckAvailability(string username, string email)
  {
    throw new NotImplementedException();
  }

  public Member? GetMemberByVerification(string verificationCode)
  {
    throw new NotImplementedException();
  }

  public void CompleteVerification(Member member, DateTimeOffset date)
  {
    throw new NotImplementedException();
  }

  public Member RefreshVerification(string username, TimeSpan verifyWindow)
  {
    throw new NotImplementedException();
  }
}