using System;
using System.Diagnostics;
using DataHelpers.Data;
using DotLiquid;
using HtmlAgilityPack;
using MemberMan;
using MemberManServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using officepark.io.API;

using officepark.io.Membership;
using Xunit;
using static MemberManServer.Mailer;

namespace MemberManTesters;

// ==========================================================================
public class ServiceTesters : TestBase
{

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This test case was provided to show that we can send a password reset email to a user, and that
  /// they can use it to complete the reset process.
  /// </summary>
  [Fact]
  public void CanResetPassword()
  {
    const string USERNAME = nameof(CanResetPassword) + "@test.com";
    const string EMAIL = USERNAME;
    const string OLD_PASSWORD = "ABC";
    const string NEW_PASSWORD = "DEF";

    SignupAndVerifyNewUser(USERNAME, EMAIL, OLD_PASSWORD, out var context);
    context.NextRequest();

    // Create a user w/ a test password.
    var dal = GetMemberAccess();
    var member = dal.GetMember(USERNAME)!;
    Assert.NotNull(member);
    Assert.True(member.IsVerified);
    Assert.False(member.IsLoggedIn);

    // NOTE: This is part of another test.  We shouldn't be able to ask to reset the password
    // if we are currently logged in!
    //// Log in the user.
    //var loginResult = context.LoginCtl.Login(new LoginModel()
    //{
    //  username = USERNAME,
    //  password = OLD_PASSWORD
    //});
    //Assert.True(loginResult.IsLoggedIn);

    // Use the reset feature + parse the email for the reset URL / code.
    var response = context.LoginCtl.ForgotPassword(USERNAME);
    Assert.True(response.Code == 0, "Invalid response code!");

    Email? lastMail = context.EmailSvc.LastEmailSent!;
    Assert.NotNull(lastMail);
    string resetCode = lastMail.Body!;
    Assert.NotNull(resetCode);
    Assert.NotEqual(string.Empty, resetCode);

    // Confirm user is logged in still.  (we don't want the reset form feature to auto logout users)
    var check = context.LoginCtl.ValidateLogin(); 
    Assert.True(check.IsLoggedIn);

    // Use the reset code + the new password.
    // Confirm that the user is now logged out!

    // Log the user in with the new password and show that it worked!
    // Show that the DB entries for reset code + times have been cleared from the system.


    // EXTRA: Create a new test case that shows that the timeout on the password reset feature works.
    Assert.True(false);
    //Assert.Fail("Finish this test please...");
  }

  // --------------------------------------------------------------------------------------------------------------------------
  [Fact]
  public void LogginInAfterPasswordResetOperationResetsIt()
  {
    Assert.True(false);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This test case shows that if a user is currently logged in, we can't log them in again.
  /// </summary>
  [Fact]
  public void CantLogInLoggedInUser()
  {
    Assert.True(false);
  }


  // --------------------------------------------------------------------------------------------------------------------------
  [Fact]
  public void CanSignupAndVerifyNewUser()
  {
    const string USER_NAME = nameof(CanSignupAndVerifyNewUser) + "@test.com";
    const string EMAIL = USER_NAME;
    const string PASS = "ABC";

    SignupNewUser(USER_NAME, EMAIL, PASS, out var context);

    // Confirm that an email was sent + get its content.
    Assert.NotNull(context.EmailSvc.LastSendResult);
    Assert.True(context.EmailSvc.LastSendResult!.SendOK);

    VerifyUser(context, USER_NAME);


    {
      Email? mail = context.EmailSvc.LastEmailSent;
      Assert.NotNull(mail);
      ConfirmVerifyCompleteMessage(mail);
    }

    // Confirm that the user is now verified in the DB.
    var dal = GetMemberAccess();
    Member? check = dal.GetMember(USER_NAME)!;
    Assert.NotNull(check);
    Assert.NotNull(check.VerifiedOn);
    Assert.Equal(DateTimeOffset.MinValue, check.VerificationExpiration);
    Assert.Null(check.VerificationCode);

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void VerifyUser(TestContext context, string username)
  {
    {
      Email? mail = context.EmailSvc.LastEmailSent;
      Assert.NotNull(mail);

      // Visit the verification URL (from the email)
      string verifyCode = GetVerificationCodeFromEmail(mail);
      var args = new VerificationArgs()
      {
        Username = username,
        VerificationCode = verifyCode,
      };
      var verifyResult = context.LoginCtl.VerifyUser(args);
      Assert.Equal(0, verifyResult.Code);
    }

  }
  // --------------------------------------------------------------------------------------------------------------------------
  private void ConfirmVerifyCompleteMessage(Email? mail)
  {
    var doc = new HtmlDocument();
    doc.LoadHtml(mail!.Body);

    // Just make sure that the link appears.
    var verificationLink = doc.DocumentNode.SelectSingleNode("//a[@class='login-link']");
    Assert.NotNull(verificationLink);

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

  // ============================================================================================================================
  class TestContext
  {
    // --------------------------------------------------------------------------------------------------------------------------
    internal TestContext(ConfigHelper config_, SimEmailService emailSvc_, LoginController loginCtl_)
    {
      Config = config_;
      EmailSvc = emailSvc_;
      LoginCtl = loginCtl_;
    }
    public ConfigHelper Config { get; private set; } = null!;
    public SimEmailService EmailSvc { get; private set; } = null!;
    public LoginController LoginCtl { get; private set; } = null!;

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Resets the state of the context, last emails sent and whatnot....
    /// </summary>
    /// NOTE: Not really sure what to name this thing.....
    internal void NextRequest()
    {
      EmailSvc.ClearLastEmail();
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SignupAndVerifyNewUser(string username, string email, string password, out TestContext context)
  {
    SignupNewUser(username, email, password, out context);
    VerifyUser(context, username);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private void SignupNewUser(string username, string email, string password, out TestContext context)
  {
    var appBuilder = WebApplication.CreateBuilder();
    var cfgHelper = Program.InitConfig(appBuilder, appBuilder.Environment);

    // Remove the test user.
    CleanupTestUser(username);

    // Create the login controller.
    // Signup the user + validate availability.
    var emailSvc = GetEmailService();
    var loginCtl = new SimLoginController(GetMemberAccess(), emailSvc, cfgHelper);
    SignupResponse response = loginCtl.Signup(new LoginModel()
    {
      username = username,
      email = email,
      password = password
    });
    Assert.Equal(0, response.Code);

    context = new TestContext(cfgHelper, emailSvc, loginCtl);
  }


  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This just shows that if a verification email sits around in the system too long, attempting to re-verify
  /// will send the email again.
  /// </summary>
  [Fact]
  public void CanResendVerificationEmail()
  {
    // Create unverified user.
    // Expire the verification (manually, probably)?
    // --> We can just get access to some internal process / service that sets this up, probably in the DAL?
    const string EMAIL = nameof(CanResendVerificationEmail) + "@test.com";
    const string NAME = EMAIL;
    const string PASS = "ABC";

    SignupNewUser(NAME, EMAIL, PASS, out var context);
    SetFakeExpiration(NAME, DateTimeOffset.Now - TimeSpan.FromDays(1));

    var mail = context.EmailSvc.LastEmailSent;
    string oldCode = GetVerificationCodeFromEmail(mail);

    // Visit Verification URL.
    var args = new VerificationArgs()
    {
      Username = NAME,
      VerificationCode = oldCode,
    };

    BasicResponse response = context.LoginCtl.VerifyUser(args);
    Assert.Equal(LoginController.VERIFICATION_EXPIRED, response.Code);


    // We are expired, so let's reverify!
    var vr = context.LoginCtl.RequestVerification(new VerificationArgs()
    {
      Username = NAME,
    }) as BasicResponse;
    Assert.NotNull(vr);
    Assert.Equal(0, vr!.Code);



    // --> Verify that we have another email with a new code.
    var newEmail = context.EmailSvc.LastEmailSent;
    string newCode = GetVerificationCodeFromEmail(newEmail);
    Assert.NotEqual(oldCode, newCode);

    // --> Show that the user is still unverified.
    var dal = GetMemberAccess();
    Member check = dal.GetMember(NAME)!;
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