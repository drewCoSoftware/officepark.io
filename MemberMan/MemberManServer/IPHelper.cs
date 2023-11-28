using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace officepark.io
{
  // ============================================================================================================================
  // NOTE: These should be extensions on 'Request' I think...
  public static class IPHelper
  {
    // --------------------------------------------------------------------------------------------------------------------------
    public static string GetIP(HttpRequest request)
    {
      var xff = request.Headers["X-Forwarded-For"];
      if (xff.Count > 0) { return xff[0]; }

      string res = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-1";
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public static string GetQuerystringValue(HttpRequest request, string name, string fallback)
    {
      StringValues vals = request.Query[name];
      string res = (vals.Count == 0) ? fallback : vals[0];

      return res;
    }


  }
}
