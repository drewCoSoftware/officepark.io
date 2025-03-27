using DotLiquid.Util;
using Microsoft.AspNetCore.Mvc;
using officepark.io;
using officepark.io.API;
using officepark.io.Membership;

namespace MemberMan;

// ============================================================================================================================
// TODO: SHARE:
public class BaseController : Controller
{

  protected string _IPAddress = default!;
  public virtual string IPAddress
  {
    get
    {
      return _IPAddress ?? (_IPAddress = IPHelper.GetIP(Request));
    }
    internal set { _IPAddress = value; }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected void RemoveCookie(string name)
  {
    Response.Cookies.Delete(name);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected void SetCookie(string name, string value, DateTime expires)
  {
    Response.Cookies.Append(name, value, new CookieOptions()
    {
      Expires = DateTime.Now + TimeSpan.FromMinutes(MembershipHelper.LOGIN_COOKIE_TIME),
      HttpOnly = false,
    });
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // TODO: Put this on a base class....
  protected string? GetCookie(string cookieName)
  {
    string? res = Request.Cookies[cookieName];
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected string ResolveUrl(string url)
  {
    if (url.StartsWith("/"))
    {
      string urlRoot = GetDomain(Request);
      string res = urlRoot + url;
      return res;
    }

    if (!url.StartsWith("https"))
    {
      throw new NotSupportedException("No support for local, non-rooted urls");
    }

    return url;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected string GetDomain(HttpRequest request)
  {
    if (request == null) { return string.Empty; }

    string res = $"{request.Scheme}://{request.Host.Host}";
    if (request.Host.Port != 80)
    {
      res += $":{request.Host.Port}";
    }
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected bool HasHeader(string headerName)
  {
    if (Request == null) { return false; }
    bool res = Request.Headers.ContainsKey(headerName);
    return res;
  }
}


// ============================================================================================================================
public class ApiController : BaseController
{



  // --------------------------------------------------------------------------------------------------------------------------
  public T OK<T>(string? message = null)
    where T : IAPIResponse, new()
  {
    T res = new T();
    res.Message = message;
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public MemberManBasicResponse OK(string? message = "OK")
  {
    return OK<MemberManBasicResponse>(message);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public MemberManBasicResponse Error(int code, string? message = "Error")
  {
    return Error<MemberManBasicResponse>(code, message);
  }
  // --------------------------------------------------------------------------------------------------------------------------
  public T Error<T>(int code, string? message = "Error")
    where T : IAPIResponse, new()
  {
    if (Response != null)
    {
      Response.StatusCode = 500;
    }
    return new T()
    {
      Code = code,
      Message = message
    };
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public IAPIResponse NotFound(string? message = null)
  {
    if (Response != null)
    {
      Response.StatusCode = 404;
    }

    return new MemberManBasicResponse()
    {
      Code = ResponseCodes.DOES_NOT_EXIST,
      Message = message
    };
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public T NotFound<T>(string? message = null)
    where T : IAPIResponse, new()
  {
    if (Response != null)
    {
      Response.StatusCode = 404;
    }

    return new T()
    {
      Code = ResponseCodes.DOES_NOT_EXIST,
      Message = message
    };
  }
}
