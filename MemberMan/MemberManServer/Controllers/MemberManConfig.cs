namespace MemberMan;

// ============================================================================================================================
/// <summary>
/// Configuration for member man and its features.
/// </summary>
public class MemberManConfig
{
  public static readonly TimeSpan DEFAULT_VERIFY_WINDOW = TimeSpan.FromHours(24);
  public static readonly TimeSpan DEFAULT_RESET_PASSWORD_WINDOW = TimeSpan.FromHours(24);

  /// <summary>
  /// The url that the user should visit to verify their account.
  /// This url is for the front end of your application and should be fully qualified.
  /// </summary>
  public string VerifyAccountUrl { get; set; } = "https://localhost/forgotverify";

  /// <summary>
  /// The url that is used to log into the account.
  /// This url is for the front end of your application and should be fully qualified.
  /// </summary>
  public string LoginUrl { get; set; } = "https://localhost/login";

  /// <summary>
  /// The url that is used to reset your password.
  /// This url is for the front end of your application and should be fully qualified.
  /// </summary>
  public string PasswordResetUrl { get; set; } = "https://localhost/fogot-password";

  public EmailServiceConfiguration EmailConfig { get; set; } = new EmailServiceConfiguration();

  public TimeSpan VerifyWindow { get; set; } = DEFAULT_VERIFY_WINDOW;

  public TimeSpan PasswordResetWindow { get; set; } = DEFAULT_RESET_PASSWORD_WINDOW;


  /// <summary>
  /// Save/Load active user data between application startup cycles.  If false, all cookie + tokens
  /// are cleared and not reloaded when the application starts up.
  /// </summary>
  public bool UseActiveUserData { get; set; } = false;
}
