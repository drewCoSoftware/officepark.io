using drewCo.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using officepark.io.Data;

namespace officepark.io.Membership;



// ============================================================================================================================
public class Member : IHasPrimary
{
  public int ID { get; set; }

  [Unique]
  public string Username { get; set; } = string.Empty;

  [Unique]
  public string Email { get; set; } = string.Empty;

  public DateTimeOffset CreatedOn { get; set; } = DateTime.MinValue;

  [Unique]
  [IsNullable]
  public string? VerificationCode { get; set; } = null;

  [IsNullable]
  public DateTimeOffset? VerificationExpiration { get; set; } = null;

  [IsNullable]
  public DateTimeOffset? VerifiedOn { get; set; } = null;

  public bool IsVerified { get { return VerifiedOn != null && VerifiedOn > CreatedOn; } }

  // TODO:
  // Indicates that the user should be prompted to change their password on the next login.
  // public bool ResetPassword { get; set; } = true;

  /// <summary>
  /// Comma delimited list of permissions, to be interpreted by the application.
  /// </summary>
  public string Permissions { get; set; } 

  /// <summary>
  /// The hashed password.
  /// </summary>
  public string Password { get; set; }

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

  public string LoginUrl { get; set; } = "/Login";

  // --------------------------------------------------------------------------------------------------------------------------
  public override void OnActionExecuting(ActionExecutingContext fc)
  {

    // This is where we will check our membership tokens and stuff.  If we don't have the right data, we will redirect.
    HttpRequest request = fc.HttpContext.Request;
    HttpResponse response = fc.HttpContext.Response;

    if (!MembershipHelper.IsLoggedIn(request))
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
      Member m = MembershipHelper.GetMember(request);
      MembershipHelper.UpdateLoginCookie(request, response);
    }

  }
}


// ============================================================================================================================
/// <summary>
/// This helps us track logged in users and stuff.
/// </summary>
public class MembershipHelper
{
  public const int LOGIN_COOKIE_TIME = 30;
  public const string MEMBERSHIP_COOKIE = "fsmid";

  private static Dictionary<string, Member> LoggedInMembers = new Dictionary<string, Member>();

  // --------------------------------------------------------------------------------------------------------------------------
  public static bool IsLoginActive(string cookieVal, string ip)
  {
    string token = GetLoginToken(cookieVal, ip);
    return LoggedInMembers.TryGetValue(token, out Member? m);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public static string CreateLoginCookie(HttpResponse response)
  {
    string val = CreateLoginCookieVal();
    response.Cookies.Append(MEMBERSHIP_COOKIE, val, new CookieOptions()
    {
      Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME),
      HttpOnly = true,
    });
    //);

    return val;
    //HttpCookie c = new HttpCookie(MEMBERSHIP_COOKIE, );
    //c.Expires = DateTime.UtcNow + TimeSpan.FromMinutes(LOGIN_COOKIE_TIME);
    //c.HttpOnly = true;

    //return c;
  }

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
    string token = GetLoginToken(request);
    bool isLoggedIn = token != null && LoggedInMembers.TryGetValue(token, out Member m);
    if (isLoggedIn)
    {
      LoggedInMembers.Remove(token);
    }
    response.Cookies.Delete(MEMBERSHIP_COOKIE);
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Generate the login token for the current member.
  /// We check this against current requests to detect cases where users are trying to hijack accounts with cookies.
  /// </summary>
  public static string CreateLoginCookieVal()
  {
    string res = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public static string GetLoginToken(HttpRequest request)
  {
    string cookie = request.Cookies[MEMBERSHIP_COOKIE];
    if (cookie == null) { return ""; }

    string res = GetLoginToken(cookie, IPHelper.GetIP(request));
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

    int len = cookie.Length;
    char[] res = new char[len];
    for (int i = 0; i < len; i++)
    {
      uint cVal = (uint)cookie[i] ^ (uint)ip[ipIndex];
      res[i] = (char)cVal;
      ipIndex = (ipIndex + 1) % ipLen;
    }

    string s = StringTools.ToHexString(new string(res));
    return s;

  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static void CompleteLoginInternal(Member m, HttpRequest request, HttpResponse response)
  {
    m.LoggedInSince = DateTime.UtcNow;
    m.LastActive = m.LoggedInSince;
    m.IsLoggedIn = true;

    // Now we create our entry for membership.
    string val = CreateLoginCookie(response);

    string token = GetLoginToken(val, m.IP);
    LoggedInMembers[token] = m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static bool IsLoggedIn(HttpRequest request)
  {
    string cookie = request.Cookies[MEMBERSHIP_COOKIE];

    bool res = cookie != null && IsLoginActive(cookie, IPHelper.GetIP(request));
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public static bool TryGetLoggedInMember(HttpRequest request, out Member member)
  {
    member = new Member()
    {
      IsLoggedIn = false
    };

    // NOTE: We are only returning a subset of the data on purpose.
    Member check = GetMember(request);
    bool res = check != null;
    if (res)
    {
      member.IsLoggedIn = true;
      member = check;
    }

    return res;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  internal static Member GetMember(HttpRequest request)
  {
    string token = GetLoginToken(request);
    Member res = null;
    if (LoggedInMembers.TryGetValue(token, out res))
    {
      res.LastActive = DateTime.UtcNow;
    }
    return res;

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


