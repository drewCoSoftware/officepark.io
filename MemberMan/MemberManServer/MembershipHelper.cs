using drewCo.Tools;
using drewCo.Tools.Logging;
using MemberMan;
using System.Diagnostics;

namespace officepark.io.Membership;

// ============================================================================================================================
/// <summary>
/// This helps us track logged in users and stuff.
/// </summary>
public class MembershipHelper
{

  public static class LogLevels { 
    public const string MM_DEBUG = "MM_DEBUG";
  }

  // OPTIONS:  This should come from member man config....
  // That also means that this helper class should also use instance methods....
  // We can worry about all that later....
  public const int LOGIN_COOKIE_TIME = 30;
  public const string MEMBERSHIP_COOKIE = "fsmid";

  protected Dictionary<string, Member> LoggedInMembers = new Dictionary<string, Member>();
  protected object DataLock = new object();

  public MemberManConfig Config { get; private set; }

  // --------------------------------------------------------------------------------------------------------------------------
  public MembershipHelper(MemberManConfig config_)
  {
    Config = config_;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public bool IsLoginActive(string cookieVal, string ip, out Member? member)
  {
    string? token = GetLoginToken(cookieVal, ip);

    Log.AddMessage(LogLevels.MM_DEBUG, $"Checking active login for ip: {ip}");
    Log.AddMessage(LogLevels.MM_DEBUG, $"Cookie is: {cookieVal}");
    Log.AddMessage(LogLevels.MM_DEBUG, $"The login token is: {token ?? "<null>"}");

    // TODO: This should be concurrent....
    lock (DataLock)
    {
      ExpireMembers();
      bool res = LoggedInMembers.TryGetValue(token, out Member? m);
      member = m;

      return res;
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will flush any members in the cache from the system.
  /// </summary>
  private void ExpireMembers()
  {
    lock (DataLock)
    {
      var now = DateTimeOffset.Now;
      var toRemove = new List<string>();
      foreach (var item in LoggedInMembers)
      {
        if (item.Value.LastActive + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME) < now)
        {
          toRemove.Add(item.Key);

        }
      }

      foreach (var item in toRemove)
      {
        LoggedInMembers.Remove(item);
        Log.AddMessage(LogLevels.MM_DEBUG, $"Member: {item} was expired!");
      }

      SaveActiveUserList();

    }
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will reload the current login data from the data store.
  /// </summary>
  /// NOTE: This could use a functor to decide where the data will come from.
  public void LoadActiveUserList(DateTimeOffset timestamp)
  {
    if (!Config.UseActiveUserData)
    {
      // VERBOSE:
      Debug.WriteLine($"{nameof(MemberManConfig.UseActiveUserData)} flag is set to false.  Active user data will not be loaded!");
      return;
    }

    lock (DataLock)
    {
      string path = ResolveActiveUsersDataPath(false);
      if (!File.Exists(path)) { return; }

      var data = FileTools.LoadJson<List<ActiveUserData>>(path);
      if (data != null)
      {
        LoggedInMembers.Clear();

        foreach (var item in data)
        {
          if (string.IsNullOrWhiteSpace(item.Username)) { continue; }
          if (item.CookieExpireTime >= timestamp)
          {
            // VERBOSE:
            Debug.WriteLine($"The cookie for member: {item.Username} expired on: {item.CookieExpireTime}.  User will not be loaded!");
            continue;
          }

          // NOTE: We might actually need to capture more information about the users.  If not, then
          // we should modify the records that we actually store in 'LoggedInMembers'
          LoggedInMembers.Add(item.Username, new Member()
          {
            Username = item.Username,
            CookieVal = item.CookieVal,
            TokenExpires = item.CookieExpireTime,
          });
        }

      }

    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Write the currentl list of logged in members to storage.
  /// This data is used on application start so that we can determine who is still active in our system.
  /// </summary>
  /// <exception cref="NotImplementedException"></exception>
  private void SaveActiveUserList()
  {
    if (!Config.UseActiveUserData)
    {
      // VERBOSE:
      Debug.WriteLine($"{nameof(MemberManConfig.UseActiveUserData)} flag is set to false.  Active user data will not be saved!");
    }

    // TEST:
    // return;

    // NOTE: we could / use a functor to determine where we actually save this data (database, filesystem, etc.)
    // For now it is easy enough to just write it all to disk.....

    // OPTIONS:
    string savePath = ResolveActiveUsersDataPath();
    var used = new HashSet<string>();

    var toWrite = new List<ActiveUserData>();
    lock (DataLock)
    {
      foreach (var item in LoggedInMembers)
      {
        if (used.Contains(item.Value.Username)) { continue; }
        used.Add(item.Value.Username);

        var val = item.Value;
        toWrite.Add(new ActiveUserData(val.Username, val.CookieVal, val.TokenExpires));
      }

      FileTools.SaveJson(savePath, toWrite);
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public string ResolveActiveUsersDataPath(bool createDir = true)
  {
    string dataDir = FileTools.GetLocalDir("Data", "Logins");
    if (createDir)
    {
      FileTools.CreateDirectory(dataDir);
    }
    string savePath = Path.Combine(dataDir, "active-users.json");
    return savePath;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public void UpdateLoginCookie(HttpRequest request, HttpResponse response)
  {

    string cookieVal = request.Cookies[MEMBERSHIP_COOKIE] ?? "";
    response.Cookies.Append(MEMBERSHIP_COOKIE, cookieVal, new CookieOptions()
    {
      Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME),
      HttpOnly = true,
    });
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void Logout(HttpRequest request, HttpResponse response)
  {
    string? token = GetLoginToken(request);
    bool isLoggedIn = token != null && LoggedInMembers.TryGetValue(token, out Member? m);
    if (isLoggedIn)
    {
      LoggedInMembers.Remove(token!);
    }
    response.Cookies.Delete(MEMBERSHIP_COOKIE);

    SaveActiveUserList();
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Generate the login token for the current member.
  /// We check this against current requests to detect cases where users are trying to hijack accounts with cookies.
  /// </summary>
  public static string CreateLoginCookie()
  {
    // NOTE: We are just throwing out some string.  This isn't particularly secure....
    string res = StringTools.ComputeMD5(Guid.NewGuid().ToString());
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public static string? GetLoginToken(HttpRequest request)
  {
    // We need the cookie, and the ip address....
    // How can we detach this from the request.....

    string? cookie = request.Cookies[MEMBERSHIP_COOKIE];
    if (cookie == null) { return null; }

    string ipAddress = IPHelper.GetIP(request);
    string res = GetLoginToken(cookie, ipAddress);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Given the current cookie value and IP address, this will create the membership token that is used to check login status.
  /// </summary>
  public static string? GetLoginToken(string? cookie, string ip)
  {
    // TODO: Convert these functions to use ILogger!

    if (cookie == null)
    {
      Log.AddMessage(LogLevels.MM_DEBUG, "The login cookie is null!");

      return null;
    }

    Console.WriteLine($"The cookie is: {cookie}!");
    Console.WriteLine($"The IP address is: {ip}!");

    int ipLen = ip.Length;
    int ipIndex = 0;

    // We encode the cookie value by x-oring it with the IP address.
    int len = cookie.Length;
    char[] encoded = new char[len];
    for (int i = 0; i < len; i++)
    {
      uint cVal = (uint)cookie[i] ^ (uint)ip[ipIndex];
      encoded[i] = (char)cVal;
      ipIndex = (ipIndex + 1) % ipLen;
    }

    string res = StringTools.ToHexString(new string(encoded));
    return res;

  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void SetLoggedInUser(Member m, string loginToken)
  {
    LoggedInMembers[loginToken] = m;
    SaveActiveUserList();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public bool IsLoggedIn(string? cookie, string ipAddress, out Member? member)
  {
    member = null;
    if (cookie == null) { return false; }
    bool res = IsLoginActive(cookie, ipAddress, out member);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public bool IsLoggedIn(HttpRequest? request, out Member? member)
  {
    member = null;
    if (request == null) { return false; }

    string? cookie = request.Cookies[MEMBERSHIP_COOKIE];
    if (string.IsNullOrEmpty(cookie)) { return false; }

    string ipAddress = IPHelper.GetIP(request);
    bool res = cookie != null && IsLoginActive(cookie, ipAddress, out member);
    return res;
  }



  // --------------------------------------------------------------------------------------------------------------------------
  public bool TryGetLoggedInMember(string? loginToken, out Member member)
  {
    if (loginToken != null)
    {
      // NOTE: We are only returning a subset of the data on purpose.
      Member? check = GetMember(loginToken);
      //Member? check = GetMember(
      if (check != null)
      {
        member = check;
        member.IsLoggedIn = true;
        return true;
      }
    }

    member = new Member();
    return false;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal Member? GetMember(HttpRequest request)
  {
    string? token = GetLoginToken(request);
    return GetMember(token);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal Member? GetMember(string? loginToken)
  {
    if (loginToken == null) { return null; }
    if (LoggedInMembers.TryGetValue(loginToken, out Member? res))
    {
      UpdateActiveUserTime(res);
    }
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void UpdateActiveUserTime(Member res)
  {
    res.LastActive = DateTime.UtcNow;

    // TODO: We aren't actually writing any of this data to the DB / active user store, and we probably should be.
    // We should also updating the expire date on any associated cookies here too!
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal bool HasRequiredPermissions(Member m, string? requiredPermissions, string? defaultScope = null)
  {
    if (!string.IsNullOrWhiteSpace(defaultScope))
    {
      throw new NotSupportedException("Permission scopes are not supported at this time!");
    }

    if (string.IsNullOrWhiteSpace(requiredPermissions)) { return true; }

    var required = ParsePermissions(requiredPermissions);
    var member = ParsePermissions(m.Permissions);


    foreach (var item in required)
    {
      // NOTE: At this time we don't actually support scopes in our permission system, so
      // We are just going to do some simple matching for them at this time.
      // Real implementations + test cases are forthcoming.
      if (member.TryGetValue(item.Key, out PermissionEntry entry))
      {
        return true;
      }
    }

    return false;
    // Now we can compare those permission sets...

    // Now we can compare those parsed permissions to whatever the user has.....
    // m.Permissions
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // NOTE:  We can think of the type 'Dictionary<string, PermissionEntry>' as a 'PermissionSet'
  private static Dictionary<string, PermissionEntry> ParsePermissions(string? requiredPermissions)
  {
    var res = new Dictionary<string, PermissionEntry>();
    if (requiredPermissions == null) { return res; }

    string[] allPerms = requiredPermissions.Split(";");
    foreach (string item in allPerms)
    {
      string[] parts = item.Split("|");
      string? scope = null;
      string perm = parts[0];

      if (parts.Length > 1)
      {
        throw new NotSupportedException("Permission scopes are not supported at this time!");
        scope = parts[0];
        perm = parts[1];
      }

      res.Add(perm, new PermissionEntry(scope, perm));
    }

    return res;
  }

  // ============================================================================================================================
  // TODO: We will use this later when we have better cookie poisoning protection...
  /// <summary>
  /// All of the data that we want to track for our login tokens....
  /// </summary>
  class LoginToken
  {
    public string Token { get; set; } = default!;
    public DateTimeOffset ExpiresOn { get; set; }
    public string IP { get; set; } = default!;
  }

  record class PermissionEntry(string? Scope, string Permission);

  public record class ActiveUserData(string Username, string CookieVal, DateTimeOffset? CookieExpireTime);

}


