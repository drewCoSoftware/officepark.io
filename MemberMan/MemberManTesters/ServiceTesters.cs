using System;
using DataHelpers.Data;
using HtmlAgilityPack;
using MemberMan;
using officepark.io.API;

using officepark.io.Membership;
using Xunit;

namespace MemberManTesters;

// ==========================================================================
public class ServiceTesters : TestBase
{
  // --------------------------------------------------------------------------------------------------------------------------
  [Fact]
  public void CanSignupAndVerifyNewUser()
  {
    const string NAME = nameof(CanSignupAndVerifyNewUser);
    const string EMAIL = NAME + "@test.com";
    const string PASS = "ABC";

    SimEmailService emailSvc;
    LoginController ctl;
    SignupNewUser(NAME, EMAIL, PASS, out emailSvc, out ctl);

    // Confirm that an email was sent + get its content.
    Assert.NotNull(emailSvc.LastSendResult);
    Assert.True(emailSvc.LastSendResult!.SendOK);

    Email? mail = emailSvc.LastEmailSent;
    Assert.NotNull(mail);

    // Visit the verification URL (from the email)
    string verifyCode = GetVerificationCodeFromEmail(mail);

    var verifyResult = ctl.VerifyUser(verifyCode);
    Assert.Equal(0, verifyResult.ResponseCode);

    // Confirm that the user is now verified in the DB.
    var dal = GetMemberAccess();
    Member? check = dal.GetMemberByName(NAME)!;
    Assert.NotNull(check);
    Assert.NotNull(check.VerifiedOn);
    Assert.Null(check.VerificationExpiration);
    Assert.Null(check.VerificationCode);

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static string GetVerificationCodeFromEmail(Email? mail)
  {
    var doc = new HtmlDocument();
    doc.LoadHtml(mail!.Body);
    var verificationLink = doc.DocumentNode.SelectSingleNode("//a[@class='verify-link']");
    Assert.NotNull(verificationLink);
    string href = verificationLink.GetAttributeValue("href", null);
    string verifyCode = href.Split("?code=")[1];    // <-- We should probably parse the url + extract querystring properly.
    return verifyCode;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SignupNewUser(string NAME, string EMAIL, string PASS, out SimEmailService emailSvc, out LoginController ctl)
  {
    // Remove the test user.
    CleanupTestUser(NAME);

    // Create the login controller.
    // Signup the user + validate availability.
    emailSvc = GetEmailService();
    ctl = new LoginController(GetMemberAccess(), emailSvc);
    SignupResponse response = ctl.Signup(new LoginModel()
    {
      username = NAME,
      email = EMAIL,
      password = PASS
    });
    Assert.Equal(0, response.ResponseCode);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This just shows that if a verification email sits around in the system too long, attempting to re-verify
  /// will send the email again.
  /// </summary>
  [Fact]
  public void ExpiredVerificationWillResendEmail()
  {
    // Create unverified user.
    // Expire the verification (manually, probably)?
    // --> We can just get access to some internal process / service that sets this up, probably in the DAL?
    const string NAME = nameof(ExpiredVerificationWillResendEmail);
    const string EMAIL = NAME + "@test.com";
    const string PASS = "ABC";

    SimEmailService emailSvc;
    LoginController ctl;
    SignupNewUser(NAME, EMAIL, PASS, out emailSvc, out ctl);
    SetFakeExpiration(NAME, DateTimeOffset.Now - TimeSpan.FromDays(1));

    var mail = emailSvc.LastEmailSent;
    string oldCode = GetVerificationCodeFromEmail(mail);

    // Visit Verification URL.
    BasicResponse response = ctl.VerifyUser(oldCode);
    Assert.Equal(LoginController.VERIFICATION_EXPIRED, response.ResponseCode);

    // --> Verify that we have another email with a new code.
    var newEmail = emailSvc.LastEmailSent;
    string newCode = GetVerificationCodeFromEmail(newEmail);
    Assert.NotEqual(oldCode, newCode);

    // --> Show that the user is still unverified.
    var dal = GetMemberAccess();
    Member check = dal.GetMemberByName(NAME)!;
    Assert.False(check.IsVerified);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private string GetVerificationCode(string name)
  {
    IDataAccess dal = ResolveDataAccess();
    string? res = dal.RunSingleQuery<string>("SELECT verificationcode FROM members where username = @name", new
    {
      name = name,
    });
    Assert.NotNull(res);

    return res!;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SetFakeExpiration(string name, DateTimeOffset expiration)
  {
    IDataAccess? dal = ResolveDataAccess();
    string query = "UPDATE members SET VerificationExpiration = @expires WHERE username = @name";
    var qParams = new
    {
      name = name,
      expires = expiration
    };

    int updated = dal.RunExecute(query, qParams);
    Assert.Equal(1, updated);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private IDataAccess ResolveDataAccess()
  {
    var dal = GetMemberAccess() as IDataAccess;
    if (dal == null)
    {
      throw new InvalidOperationException($"The MemberAccess provider does not implement the '{nameof(IDataAccess)}' interface!");
    }

    return dal;
  }
}