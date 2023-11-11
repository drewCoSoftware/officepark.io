using MemberManServer;
using System.Diagnostics;
using System.Net;
using static MemberManServer.Mailer;

// ==========================================================================      
public class EmailSendResult
{
  public bool SendOK = false;
}

//// ==========================================================================      
//public class Email
//{
//  // CONTENT TBD.
//  public string Body { get; set; } = string.Empty;
//  public bool IsHtml { get; set; } = false;
//}

// ==========================================================================      
public interface IEmailService
{
  EmailSendResult SendEmail(Email mail);
}

// ==========================================================================      
public class EmailService : IEmailService
{
  private string SmtpHost = null!;
  private int Port = 0;
  private string User = null!;
  private string Password = null!;

  // --------------------------------------------------------------------------------------------------------------------------
  public EmailService(string smtpHost_, int port_, string user, string password)
  {
    SmtpHost = smtpHost_;
    Port = port_;
    User = user;
    Password = password;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // NOTE: Username/pass should be passed into this function!
  public EmailSendResult SendEmail(Email mail)
  {
    try
    {
      // TEMP: We are testing some email features....
      //Email mail = new Email(EMAIL_FROM, "drew@august-harper.com", "test", "This is a test!", false);

      //const string PASSWORD = "your password here!";
      var creds = new NetworkCredential(User, Password);
      Mailer.SendMail(mail, SmtpHost, Port, false, creds);

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
}