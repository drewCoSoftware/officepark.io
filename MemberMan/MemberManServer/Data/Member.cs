using DataHelpers.Data;
using drewCo.Tools;
using MemberMan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;


namespace officepark.io.Membership;



// ============================================================================================================================
public class Member : IHasPrimary
{
  public int ID { get; set; }

  [Unique]
  public string Username { get; set; } = null!;

  [Unique]
  public string Email { get; set; } = null!;

  public DateTimeOffset CreatedOn { get; set; } = DateTime.MinValue;

  [Unique]
  [IsNullable]
  public string? VerificationCode { get; set; } = null;

  public DateTimeOffset VerificationExpiration { get; set; } = DateTime.MinValue;

  [IsNullable]
  public DateTimeOffset? VerifiedOn { get; set; } = null;

  public bool IsVerified { get { return VerifiedOn != null && VerifiedOn > CreatedOn; } }

  // TODO:
  // Indicates that the user should be prompted to change their password on the next login.
  // public bool ResetPassword { get; set; } = true;

  /// <summary>
  /// Comma delimited list of permissions, to be interpreted by the application.
  /// </summary>
  public string? Permissions { get; set; } = null;

  /// <summary>
  /// The hashed password.
  /// </summary>
  public string Password { get; set; } = null!;

  // This data is used with the current session.
  [JsonIgnore]
  public DateTimeOffset LoggedInSince { get; set; }
  [JsonIgnore]
  public DateTimeOffset LastActive { get; set; }
  [JsonIgnore]
  public string IP { get; set; }
  [JsonIgnore]
  public string CookieVal { get; set; }

  [JsonIgnore]
  public bool IsLoggedIn { get; set; } = false;
}


// ============================================================================================================================
public class CheckMembership : ActionFilterAttribute
{
  public const string REDIRECT_TO_GETVAR = "redirecto";

  /// <summary>
  /// This will change a failed check to return a 404 in cases where the user is not logged in.
  /// The purpose of this is to conceal possibly valid urls to robots and miscreants.
  /// </summary>
  public bool Show404OnLoggedOut { get; set; } = false;

  // OPTION: How do we give it settings when it is an attribute?
  public string LoginUrl { get; set; } = "/Login";

  // --------------------------------------------------------------------------------------------------------------------------
  public override void OnActionExecuting(ActionExecutingContext fc)
  {



    // This is where we will check our membership tokens and stuff.  If we don't have the right data, we will redirect.
    HttpRequest request = fc.HttpContext.Request;
    HttpResponse response = fc.HttpContext.Response;

    bool isLoggedIn = false;
    IMemberManFeatures? ctl = fc.Controller as IMemberManFeatures;
    if (ctl != null)
    {
      isLoggedIn = MembershipHelper.IsLoggedIn(request);
    }


    if (!isLoggedIn)
    {
      if (Show404OnLoggedOut)
      {
        throw new HttpException(404, "Not Found");
      }
      // Redirect to homepage..?? (maybe a 404 to better hide the existance of such features?  That could be hard to implement tho...)
      response.Cookies.Delete(MembershipHelper.MEMBERSHIP_COOKIE);

      string useUrl = LoginUrl + $"?{REDIRECT_TO_GETVAR}={HttpUtility.UrlEncode(request.Path)}";
      fc.Result = new RedirectResult(useUrl);

      return;
    }
    else
    {
      // The cookie is good, so we will make sure that we have a valid login handle.  If we do, then we can
      // update the window time of the login cookie.
      Member? m = MembershipHelper.GetMember(request);
      MembershipHelper.UpdateLoginCookie(request, response);

      // TODO: The 'last visited / active data' in the db should be updated here...?
    }

  }
}


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
  public static string GetLoginToken(string cookie, string ip)
  {
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
}






// ============================================================================================================================
// NOTE: This is .netcore specific.
public class HttpException : Exception
{
  public int StatusCode { get; private set; }
  public HttpException(int statusCode, string msg) : base(msg)
  {
    StatusCode = statusCode;
  }
}


