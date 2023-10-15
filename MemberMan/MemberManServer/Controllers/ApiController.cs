using Microsoft.AspNetCore.Mvc;
using officepark.io.API;

namespace MemberMan;

// ============================================================================================================================
public class ApiController : Controller
{
  // --------------------------------------------------------------------------------------------------------------------------
  public IAPIResponse NotFound(string message)
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
  public T NotFound<T>(string message)
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
