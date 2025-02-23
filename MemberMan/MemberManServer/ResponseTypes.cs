using officepark.io.API;

namespace MemberMan;


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

// ============================================================================================================================
public class MemberManBasicResponse : IAPIResponse
{
  public string? AuthToken { get; set; }
  public bool AuthRequired { get; set; } = true;
  public string? Message { get; set; }
  public int Code { get; set; } = 0;
}

// ============================================================================================================================
public class SignupResponse : MemberManBasicResponse
{
  public bool IsUsernameAvailable { get; set; }
  public bool IsEmailAvailable { get; set; }
}

// ============================================================================================================================
public class LoginResponse : MemberManBasicResponse
{
  /// <summary>
  /// Is the user logged in?
  /// </summary>
  public bool IsLoggedIn { get; set; }

  /// <summary>
  /// Is this a verified user?  Depending on the application, the user may or may not be allowed to 
  /// access certain features or even the entire system.
  /// </summary>
  public bool IsVerified { get; set; }

  /// <summary>
  /// The name that should be displayed in a UI.  This doesn't have to be the same thing
  /// as the username used on login.
  /// </summary>
  public string DisplayName { get; set; } = default!;

  /// <summary>
  /// Url to user avatar.  Can be an image, gravatar, whatever....
  /// </summary>
  public string? Avatar { get; set; } = null;
}