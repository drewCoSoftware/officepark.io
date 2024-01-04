using System.Net;
using MailKit.Net.Smtp;
using MimeKit;

namespace MemberManServer
{

  // ============================================================================================================================
  /// <summary>
  /// This class is used to send email.
  /// </summary>
  public static class Mailer
  {
    // --------------------------------------------------------------------------------------------------------------------------
    // NOTE : This functionality can be merged in with our email service classes.
    public static void SendMail(Email mail, string smtpHost, int? port = null, bool highPriority = false, NetworkCredential creds = null)
    {
      // Send with mailkit....
      try
      {
        var msg = new MimeMessage();

        msg.From.Add(new MailboxAddress(mail.From.Name, mail.From.Address));
        foreach (var item in mail.To)

        {
          var addr = new MailboxAddress(item.Name, item.Address);
          msg.To.Add(addr);
        }

        msg.Subject = mail.Subject;

        var builder = new BodyBuilder();
        if (mail.IsHtml)
        {
          builder.HtmlBody = mail.Body;
        }
        else
        {
          builder.TextBody = mail.Body;
        }

        if (mail.Attachments != null)
        {
          foreach (var item in mail.Attachments)
          {
            builder.Attachments.Add(item);
          }
        }


        msg.Body = builder.ToMessageBody();


        using (var client = new SmtpClient())
        {
          client.Connect(smtpHost, port ?? 22, true);

          if (creds != null)
          {
            client.Authenticate(creds);
          }

          client.Send(msg);
          client.Disconnect(true);
        }


      }
      catch (Exception ex)
      {
        Console.WriteLine("Could not send the email due to an error.....");
        Console.Write(ex.Message);
        throw new SendEmailException("Email send failed!", ex);
      }


    }
  }

  // ============================================================================================================================
  public class EmailAddress
  {
    public EmailAddress(string name_, string address_)
    {
      Name = name_;
      Address = address_;
    }

    public EmailAddress(string address_)
      : this(null, address_)
    { }

    public string? Name { get; private set; }
    public string Address { get; private set; }
  }


  // ============================================================================================================================
  /// <summary>
  /// Encapsulates all of the pertinnet information about an email that we want to send.
  /// </summary>
  public class Email
  {
    //  // --------------------------------------------------------------------------------------------------------------------------
    //  public Email()
    //  {
    //    To = new List<string>();
    //    CC = new List<string>();
    //    Bcc = new List<string>();
    //    Attachment = new List<string>();
    //  }

    // --------------------------------------------------------------------------------------------------------------------------
    public Email(string from_, string to_, string subject_, string body_, bool isHtml_)
    {
      From = new EmailAddress(from_);
      To.Add(new EmailAddress(to_));
      Subject = subject_;
      Body = body_;
      IsHtml = isHtml_;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public Email(EmailAddress from, List<EmailAddress> to, string? subject, string? body, bool isHtml)
    {
      From = from;
      To = to;
      Subject = subject;
      Body = body;
      IsHtml = isHtml;
    }

    public EmailAddress From { get; set; }
    public List<EmailAddress> To { get; set; } = new List<EmailAddress>();
    public List<EmailAddress> CC { get; set; } = new List<EmailAddress>();
    public List<EmailAddress> Bcc { get; set; } = new List<EmailAddress>();
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public bool IsHtml { get; set; }

    /// <summary>
    /// A List of file paths that can be attached to the email.
    /// </summary>
    public List<string> Attachments { get; set; } = new List<string>();
  }


  // ============================================================================================================================
  [Serializable]
  public class SendEmailException : Exception
  {
    public SendEmailException() { }
    public SendEmailException(string message) : base(message) { }
    public SendEmailException(string message, Exception inner) : base(message, inner) { }
    protected SendEmailException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context)
        : base(info, context) { }
  }


}