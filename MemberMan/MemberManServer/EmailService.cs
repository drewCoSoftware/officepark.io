
using System.Diagnostics;

// ==========================================================================      
public class EmailSendResult
{
  public bool SendOK = false;
}

// ==========================================================================      
public class Email
{
  // CONTENT TBD.
}

// ==========================================================================      
public interface IEmailService
{
  EmailSendResult SendEmail(Email mail);
}

// ==========================================================================      
public class EmailService : IEmailService
{
  // --------------------------------------------------------------------------------------------------------------------------
  public EmailSendResult SendEmail(Email mail)
  {
    Debug.WriteLine("Sending email is not currently enabled!");
    var res = new EmailSendResult()
    {
      SendOK = true
    };
    return res;
  }
}