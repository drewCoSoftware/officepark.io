using MemberMan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Web;


namespace officepark.io.Membership;

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


  /// <summary>
  /// Semicolon delimited list of permissions that are required to use the resource, formatted as:
  /// [SCOPE]|PERMISSION;...
  /// </summary>
  public string? RequiredPermissions { get; set; } = null;

  // --------------------------------------------------------------------------------------------------------------------------
  public override void OnActionExecuting(ActionExecutingContext context)
  {
    // This is where we will check our membership tokens and stuff.  If we don't have the right data, we will redirect.
    HttpRequest request = context.HttpContext.Request;
    HttpResponse response = context.HttpContext.Response;

    bool isLoggedIn = HandleLoginCheck(context, request);

    if (!isLoggedIn)
    {
      if (Show404OnLoggedOut)
      {
        throw new HttpException(404, "Not Found");
      }
      HandleNotLoggedIn(context, request, response);
    }
    else
    {
      // The cookie is good, so we will make sure that we have a valid login handle.  If we do, then we can
      // update the window time of the login cookie.
      Member? m = MembershipHelper.GetMember(request);
      MembershipHelper.UpdateLoginCookie(request, response);

      // TODO: The 'last visited / active data' in the db should be updated here...?


      // Permissions check?
      if (!string.IsNullOrWhiteSpace(RequiredPermissions) &&
          !HasPermission(context, m, RequiredPermissions))
      {
        HandleMissingPermissions(context);
      }
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual bool HasPermission(ActionExecutingContext context, Member m, string? requiredPermissions)
  {
    // Since we just pulled a fresh copy of the member from DB / memory, we can
    // check the permissions directly....
    bool res = MembershipHelper.HasPermission(m, requiredPermissions);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual bool HandleLoginCheck(ActionExecutingContext context, HttpRequest request)
  {
    bool res = false;
    IMemberManFeatures? ctl = context.Controller as IMemberManFeatures;
    if (ctl == null)
    {
      string msg = $"This controller does not implement the {nameof(IMemberManFeatures)} features interface and can't be used to check login status!";
      msg += Environment.NewLine + $"Please implement the interface, or override the {nameof(HandleLoginCheck)} function in a {nameof(CheckMembership)} subclass!";
      Debug.WriteLine(msg);
    }
    res = MembershipHelper.IsLoggedIn(request);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual void HandleMissingPermissions(ActionExecutingContext context)
  {
    context.Result = new ContentResult()
    {
      StatusCode = 401,
      Content = "Not Authorized"
    };
  }



  // --------------------------------------------------------------------------------------------------------------------------
  protected virtual void HandleNotLoggedIn(ActionExecutingContext context, HttpRequest request, HttpResponse response)
  {
    // Redirect to homepage..?? (maybe a 404 to better hide the existance of such features?  That could be hard to implement tho...)
    response.Cookies.Delete(MembershipHelper.MEMBERSHIP_COOKIE);

    string useUrl = LoginUrl + $"?{REDIRECT_TO_GETVAR}={HttpUtility.UrlEncode(request.Path)}";
    context.Result = new RedirectResult(useUrl);

    //return;
  }
}


