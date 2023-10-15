namespace officepark.io.API;

// ============================================================================================================================
/// <summary>
/// This interface type should be implemented by all return types in the system.
/// The goal is to have common data to program against for messages, errors, auth status, etc.
/// </summary>
public interface IAPIResponse
{
  /// <summary>
  /// The user's current auth token.  If null, then the user is not currently
  /// authorized and should login again.
  /// </summary>
  string? AuthToken { get; set; }

  /// <summary>
  /// Indicates if the request in question requires authorization.
  /// </summary>
  bool AuthRequired { get; set; }

  /// <summary>
  /// Any old message that we want to send along.
  /// </summary>
  /// <value></value>
  string? Message { get; set; }

  /// <summary>
  /// Extra response code, if any to indicate the result of the operation.
  /// A repsonse code of zero indicates that everything is OK.
  /// </summary>
  int Code { get; set; }
}

// ============================================================================================================================
public class BasicResponse : IAPIResponse
{
  public string? AuthToken { get; set; }
  public bool AuthRequired { get; set; } = true;
  public string? Message { get; set; }
  public int Code { get; set; } = 0;
}

// ============================================================================================================================
public static class ResponseCodes
{
  public const int OK = 0;

  /// <summary>
  /// The data in question does not exist.
  /// </summary>
  public const int DOES_NOT_EXIST = 1;

  /// <summary>
  /// The data already exists.
  /// </summary>
  public const int ALREADY_EXISTS = 2;

  /// <summary>
  /// Data passed to the API did not pass validation.
  /// </summary>
  public const int INVALID_DATA = 3;

  /// <summary>
  /// The current process/request is already in progress.
  /// </summary>
  public const int IN_PROGRESS = 4;

  /// <summary>
  /// User is not authoerized to perform the specified action!
  /// </summary>
  public const int NOT_AUTHORIZED = 5;
}