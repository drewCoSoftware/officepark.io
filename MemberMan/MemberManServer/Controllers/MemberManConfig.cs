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
  /// </summary>
  public string VerifyAccountUrl { get; set; } = "https://localhost/fogotverify";

  /// <summary>
  /// The url that is used to log into the account.
  /// </summary>
  public string LoginUrl { get; set; } = "https://localhost/login";

  /// <summary>
  /// The url that is used for password reset. 
  /// </summary>
  public string PasswordResetUrl { get; set; } = "https://localhost/fogot-password";

  /// <summary>
  /// Email account that sends verification emails.
  /// </summary>
  public string VerificationSender { get; set; } = default!;

  /// <summary>
  /// The server address that emails are sent through....
  /// </summary>
  public string SmtpServer { get; set; } = default!;
  public int SmtpPort { get; set; } = 465;
  public string SmtpPassword { get; set; } = default!;

  public TimeSpan VerifyWindow { get; set; } = DEFAULT_VERIFY_WINDOW;

  public TimeSpan PasswordResetWindow { get; set; } = DEFAULT_RESET_PASSWORD_WINDOW;
}
