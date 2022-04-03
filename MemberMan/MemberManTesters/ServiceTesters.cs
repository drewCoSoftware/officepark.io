using System;
using HtmlAgilityPack;
using MemberMan;
using officepark.io.Membership;
using Xunit;

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

    // Remove the test user.
    CleanupTestUser(NAME);

    // Create the login controller.
    // Signup the user + validate availability.
    SimEmailService emailSvc = GetEmailService();
    var ctl = new LoginController(GetMemberAccess(), emailSvc);
    SignupResponse response = ctl.Signup(new LoginModel()
    {
      username = NAME,
      email = EMAIL,
      password = PASS
    });
    Assert.Equal(0, response.ResponseCode);

    // Confirm that an email was sent + get its content.
    Assert.NotNull(emailSvc.LastSendResult);
    Assert.True(emailSvc.LastSendResult!.SendOK);

    Email? mail = emailSvc.LastEmailSent;
    Assert.NotNull(mail);

    // Visit the verification URL (from the email)
    var doc = new HtmlDocument();
    doc.LoadHtml(mail!.Body);
    var verificationLink = doc.DocumentNode.SelectSingleNode("//a[@class='verify-link']");
    Assert.NotNull(verificationLink);

    string href = verificationLink.GetAttributeValue("href", null);
    string verifyCode = href.Split("?code=")[1];    // <-- We should probably parse the url + extract querystring properly.

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

    // Visit Verification URL.
    // --> We should have another email.
    // --> Show that the user is still unverified.

    throw new NotImplementedException("Please finish this test!");
  }

}