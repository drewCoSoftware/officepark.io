﻿using Microsoft.AspNetCore.Mvc;
using officepark.io.API;
using officepark.io.Membership;

namespace MemberMan;

// ============================================================================================================================
public class ApiController : Controller
{


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
  protected bool HasHeader(string headerName)
  {
    if (Request == null) { return false; }
    bool res = Request.Headers.ContainsKey(headerName);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public T OK<T>(string? message = null)
    where T:IAPIResponse, new()
  {
    T res = new T();
    res.Message = message;
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public BasicResponse OK(string? message = "OK")
  {
    return OK<BasicResponse>(message);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public IAPIResponse NotFound(string? message = null)
  {
    if (Response != null)
    {
      Response.StatusCode = 404;
    }

    return new BasicResponse()
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
