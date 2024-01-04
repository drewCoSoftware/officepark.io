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

  ///// <summary>
  ///// Email account that sends verification emails.
  ///// </summary>
  //public string EmailConfig.Username { get; set; } = default!;

  ///// <summary>
  ///// The server address that emails are sent through....
  ///// </summary>
  //public string SmtpServer { get; set; } = default!;
  //public int SmtpPort { get; set; } = 465;
  //public string SmtpPassword { get; set; } = default!;

  public EmailServiceConfiguration EmailConfig { get; set; } = new EmailServiceConfiguration();

  public TimeSpan VerifyWindow { get; set; } = DEFAULT_VERIFY_WINDOW;

  public TimeSpan PasswordResetWindow { get; set; } = DEFAULT_RESET_PASSWORD_WINDOW;
}
