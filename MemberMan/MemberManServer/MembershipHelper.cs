using drewCo.Tools;


namespace officepark.io.Membership;

// ============================================================================================================================
/// <summary>
/// This helps us track logged in users and stuff.
/// </summary>
public class MembershipHelper
{
  // OPTIONS:  This should come from member man config....
  // That also means that this helper class should also use instance methods....
  // We can worry about all that later....
  public const int LOGIN_COOKIE_TIME = 30;
  public const string MEMBERSHIP_COOKIE = "fsmid";

  private static Dictionary<string, Member> LoggedInMembers = new Dictionary<string, Member>();

  private static object DataLock = new object();

  // --------------------------------------------------------------------------------------------------------------------------
  public static bool IsLoginActive(string cookieVal, string ip)
  {
    string token = GetLoginToken(cookieVal, ip);

    // TODO: This should be concurrent....
    lock (DataLock)
    {
      ExpireMembers();
      bool res = LoggedInMembers.TryGetValue(token, out Member? m);
      return res;
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This will flush any members in the cache from the system.
  /// </summary>
  private static void ExpireMembers()
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
      }
    }
  }

  // // --------------------------------------------------------------------------------------------------------------------------
  // public static string CreateLoginCookie(HttpResponse response)
  // {
  //   string val = CreateLoginCookieVal();
  //   response.Cookies.Append(MEMBERSHIP_COOKIE, val, new CookieOptions()
  //   {
  //     Expires = DateTime.Now + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME),
  //     HttpOnly = false,
  //   });
  //   //);

  //   return val;
  //   //HttpCookie c = new HttpCookie(MEMBERSHIP_COOKIE, );
  //   //c.Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME);
  //   //c.HttpOnly = true;

  //   //return c;
  // }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static void UpdateLoginCookie(HttpRequest request, HttpResponse response)
  {
    //      HttpCookie c = response.Cookies[MEMBERSHIP_COOKIE];
    //HttpCookie res = new HttpCookie(MEMBERSHIP_COOKIE, request.Cookies[MEMBERSHIP_COOKIE]?.Value);
    //res.Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME);
    //res.HttpOnly = true;

    string cookieVal = request.Cookies[MEMBERSHIP_COOKIE] ?? "";
    response.Cookies.Append(MEMBERSHIP_COOKIE, cookieVal, new CookieOptions()
    {
      Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME),
      HttpOnly = true,
    });

    // return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static void Logout(HttpRequest request, HttpResponse response)
  {
    string? token = GetLoginToken(request);
    bool isLoggedIn = token != null && LoggedInMembers.TryGetValue(token, out Member? m);
    if (isLoggedIn)
    {
      LoggedInMembers.Remove(token!);
    }
    response.Cookies.Delete(MEMBERSHIP_COOKIE);
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
    if (cookie == null) { return null; }

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
  internal static void SetLoggedInUser(Member m, string loginToken)
  {
    LoggedInMembers[loginToken] = m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static bool IsLoggedIn(string? cookie, string ipAddress)
  {
    if (cookie == null) { return false; }
    bool res = IsLoginActive(cookie, ipAddress);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static bool IsLoggedIn(HttpRequest? request)
  {
    if (request == null) { return false; }

    string? cookie = request.Cookies[MEMBERSHIP_COOKIE];
    if (string.IsNullOrEmpty(cookie)) { return false; }

    string ipAddress = IPHelper.GetIP(request);
    bool res = cookie != null && IsLoginActive(cookie, ipAddress);
    return res;
  }



  // --------------------------------------------------------------------------------------------------------------------------
  public static bool TryGetLoggedInMember(string? loginToken, out Member member)
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
  internal static Member? GetMember(HttpRequest request)
  {
    string? token = GetLoginToken(request);
    return GetMember(token);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static Member? GetMember(string? loginToken)
  {
    if (loginToken == null) { return null; }
    if (LoggedInMembers.TryGetValue(loginToken, out Member? res))
    {
      res.LastActive = DateTime.UtcNow;
    }
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static bool HasPermission(Member m, string? requiredPermissions, string? defaultScope = null)
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

}


