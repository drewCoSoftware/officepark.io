using MemberManServer;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using static MemberManServer.Mailer;

// ==========================================================================      
public class EmailSendResult
{
  public bool SendOK = false;
}

// ==========================================================================      
public interface IEmailService
{
  EmailSendResult SendEmail(Email mail);
}

// ==========================================================================      
public class EmailServiceConfiguration
{
  public string SmtpServer { get; set; } = default!;
  public int Port { get; set; } = 465;
  public string Username { get; set; } = default!;
  public string Password { get; set; } = default!;

  /// <summary>
  /// When set, these are the domains that we are allowed to send email to.
  /// ALL other domains will be disallowed when this is set.
  /// </summary>
  public List<string> AllowSendToDomains { get; set; } = new List<string>();

  /// <summary>
  /// When set, these are the domains that we are not allowed to send email to.
  /// </summary>
  public List<string> DisallowSendToDomains { get; set; } = new List<string>();
}

// ==========================================================================      
/// <summary>
/// Use this email service when you don't want to actually send email!
/// </summary>
public class NullEmailService : IEmailService
{
  // --------------------------------------------------------------------------------------------------------------------------
  public EmailSendResult SendEmail(Email mail)
  {
    // TOOD: Convert this so that it uses 'Logger'.
    // Maybe 'Logger' can have some feature where it can log files?
    Console.WriteLine("null email service is sending an email!");

    return new EmailSendResult()
    {
      SendOK = true
    };

  }
}

// ==========================================================================      
public class EmailService : IEmailService
{
  private EmailServiceConfiguration Options = default!;

  // --------------------------------------------------------------------------------------------------------------------------
  public EmailService(EmailServiceConfiguration options_)
  {
    Options = options_;
    ValidateOptions();
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void ValidateOptions()
  {
    if (string.IsNullOrEmpty(Options.Username))
    {
      throw new ArgumentNullException("Username may not be null or empty!");
    }

    if (string.IsNullOrEmpty(Options.Password))
    {
      throw new ArgumentNullException("Password may not be null or empty!");
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // NOTE: Username/pass should be passed into this function!
  public EmailSendResult SendEmail(Email mail)
  {
    try
    {

      // Check the allow / disallow lists....
      ValidateToAddresses(mail.To);

      //// NOTE: This could be some kind of trace?
      //Console.WriteLine("Sending mail with:");
      //Console.WriteLine(JsonSerializer.Serialize(new
      //{
      //  Options.SmtpServer,
      //  Options.Port,
      //  Options.Username,
      //  Options.Password    // NOTE: HIDE THE PASSWORD! (stringtools?)
      //}, new JsonSerializerOptions() { WriteIndented = true }));

      var creds = new NetworkCredential(Options.Username, Options.Password);
      Mailer.SendMail(mail, Options.SmtpServer, Options.Port, false, creds);

    }
    catch (Exception ex)
    {
      // TODO: Log the error + alert!
      throw;
    }

    var res = new EmailSendResult()
    {
      SendOK = true
    };
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public static string? GetEmailDomain(string emailAddress)
  {
    var parts = emailAddress.Split("@");
    if (parts.Length < 2)
    {
      return null;
    }
    string res = parts.Last();
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void ValidateToAddresses(List<EmailAddress> to)
  {
    if (Options.AllowSendToDomains.Count > 0)
    {
      foreach (var addr in to)
      {
        string? domain = GetEmailDomain(addr.Address);
        if (domain != null && !Options.AllowSendToDomains.Contains(domain))
        {
          throw new InvalidOperationException($"The email address: {addr.Address} is in a domain that is not allowed!");
        }
      }
    }
    if (Options.DisallowSendToDomains.Count > 0)
    {
      foreach (var addr in to)
      {
        string? domain = GetEmailDomain(addr.Address);
        if (domain != null && Options.DisallowSendToDomains.Contains(domain))
        {
          throw new InvalidOperationException($"The email address: {addr.Address} is in a domain that is not allowed!");
        }
      }
    }
  }
}