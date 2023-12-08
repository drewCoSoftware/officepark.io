
// ==========================================================================
using MemberManServer;
using System;

public class SimEmailService : IEmailService
{
  /// <summary>
  /// The last email that was sent by the service.
  /// </summary>
  public Email? LastEmailSent { get; set; } = null;
  public EmailSendResult? LastSendResult { get; set; } = null;

  // --------------------------------------------------------------------------------------------------------------------------
  public EmailSendResult SendEmail(Email mail)
  {
    var res = new EmailSendResult()
    {
      SendOK = true
    };

    LastEmailSent = mail;
    LastSendResult = res;
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal void ClearLastEmail()
  {
    LastEmailSent = null; 
    LastSendResult = null;  
  }
}