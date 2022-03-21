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
  }

  // ============================================================================================================================
  public class BasicResponse : IAPIResponse
  {
    public string? AuthToken { get; set; }
    public bool AuthRequired { get; set; } = true;
    public string? Message { get; set; }
  }
