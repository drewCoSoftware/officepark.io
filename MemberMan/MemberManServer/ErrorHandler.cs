using officepark.io.API;
using System.Net;
using System.Text.Json;

namespace MemberManServer
{
  // ============================================================================================================================
  // TODO: This needs a real name, and it needs to live in shared space so that other web-apps can make use of it.
  public class ErrorHandler
  {
    private readonly RequestDelegate next;

    // --------------------------------------------------------------------------------------------------------------------------
    public ErrorHandler(RequestDelegate next)
    {
      this.next = next;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public async Task Invoke(HttpContext context)
    {
      try
      {
        await next(context);
      }
      catch (Exception ex)
      {
        await HandleException(context, ex);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static Task HandleException(HttpContext context, Exception ex)
    {
      HttpStatusCode code = HttpStatusCode.InternalServerError; // 500 if unexpected

      var result = new BasicResponse()
      {
        AuthRequired = false,
        AuthToken = null,
        Code = ResponseCodes.INVALID_DATA,
        Message = ex.Message
      };
      string content = JsonSerializer.Serialize(result);

      // TODO: Some kind of plugin for actual logging on the server?
      // What other information can we get about this error?

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)code;

      return context.Response.WriteAsync(content);
    }
  }
}
