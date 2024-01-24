using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Threading.Tasks.Dataflow;
using DataHelpers.Data;
using DotLiquid;
using drewCo.Tools;
using HtmlAgilityPack;
using MemberMan;
using MemberManServer;
using MemberManTesters.SimTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using officepark.io.API;

using officepark.io.Membership;
using Xunit;
using static MemberManServer.Mailer;

namespace MemberManTesters;

// ==========================================================================
public partial class ServiceTesters : TestBase
{

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This test case was provided to solve an issue where restarting the server that runs MemberMan will lose information
  /// about which users may be currently logged in.
  /// </summary>
  [Fact]
  public void MembershipHelperCanPreserveStateBetweenActivations()
  {
    const string USERNAME = nameof(CantLogInLoggedInUser) + "@test.com";
    const string EMAIL = USERNAME;
    const string PASSWORD = "DEF";

    SignupAndVerifyNewUser(USERNAME, EMAIL, PASSWORD, out var context);
    context.NextRequest();

    var mmCfg = context.Config.Get<MemberManConfig>();

    // JFC, its 2024 and the dopes at XUnit still have the library in alpha mode.
    // Not really trying to do all that, so we will just deal with this test case appearing as tho it is failed!
    Skip.IfNot(mmCfg.UseActiveUserData, "INCONCLUSIVE! The config is set to not use active user data.  This test can't be fully evaluated!");

    var loginResponse = context.LoginCtl.Login(new LoginModel()
    {
      username = USERNAME,
      password = PASSWORD
    });
    Assert.True(loginResponse.Code == 0);
    Assert.True(context.MembershipHelper.IsUserActive(USERNAME));


    // This test context should also have the logged in user data!
    var context2 = CreateTestContext();
    bool isActive = context2.MembershipHelper.IsUserActive(USERNAME);
    Assert.True(isActive, $"The user '{USERNAME}' should be marked as active!");
  }



  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This test case shows that if a user is currently logged in, we get a response indicating as such.
  /// </summary>
  [Fact]
  public void CantLogInLoggedInUser()
  {
    const string USERNAME = nameof(CantLogInLoggedInUser) + "@test.com";
    const string EMAIL = USERNAME;
    const string PASSWORD = "DEF";

    SignupAndVerifyNewUser(USERNAME, EMAIL, PASSWORD, out var context);
    context.NextRequest();

    {
      var res = context.LoginCtl.Login(new LoginModel()
      {
        username = USERNAME,
        password = PASSWORD
      });
      Assert.Equal(0, res.Code);
      Assert.True(res.IsLoggedIn, "We should be logged in!");
    }

    // Now login again.  We should get a code indicating that we are already there....
    {
      var res = context.LoginCtl.Login(new LoginModel()
      {
        username = USERNAME,
        password = PASSWORD
      });
      Assert.True(res.IsLoggedIn, "We should be logged in!");
      Assert.Equal(LoginController.LOGGED_IN_CODE, res.Code);
    }


  }


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

    // NOTE: This is part of another test that we should write.  We shouldn't be able to ask to reset the password
    // if we are currently logged in!
    //// Log in the user.
    //var loginResult = context.LoginCtl.Login(new LoginModel()
    //{
    //  username = USERNAME,
    //  password = OLD_PASSWORD
    //});
    //Assert.True(loginResult.IsLoggedIn);

    // Use the reset feature + parse the email for the reset URL / code.
    var response = context.LoginCtl.ForgotPassword(new ForgotPasswordArgs()
    {
      Username = USERNAME
    });
    Assert.True(response.Code == 0, "Invalid response code!");

    Email? lastMail = context.EmailSvc.LastEmailSent!;
    Assert.NotNull(lastMail);
    string resetCode = lastMail.Body?.Split("?resetToken=")[1]!;
    Assert.NotNull(resetCode);
    Assert.NotEqual(string.Empty, resetCode);

    // Use the reset code + the new password.
    // Confirm that the user is now logged out!
    context.NextRequest();
    var res = context.LoginCtl.ResetPassword(new ResetPasswordArgs(resetCode, NEW_PASSWORD, NEW_PASSWORD));
    Assert.Equal(0, res.Code);

    // Log the user in with the new password and show that it worked!

    {
      var vr = context.LoginCtl.ValidateLogin();
      Assert.False(vr.IsLoggedIn, "The user should not be logged in!");
    }
    {
      var vr = context.LoginCtl.Login(new LoginModel()
      {
        username = USERNAME,
        password = OLD_PASSWORD
      });
      Assert.False(vr.IsLoggedIn, "The user should not be logged in after using the old password");
    }
    {
      var vr = context.LoginCtl.Login(new LoginModel()
      {
        username = USERNAME,
        password = NEW_PASSWORD
      });
      Assert.True(vr.IsLoggedIn, "The user should be logged in after using the new password");
    }


    // Show that the DB entries for reset code + times have been cleared from the system.
    Member? check = dal.GetMember(USERNAME);
    Assert.NotNull(check);
    Assert.Null(check?.ResetToken);
    Assert.Null(check?.TokenExpires);

  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// This test case shows that if we request a password reset, and then log in, the reset data will be
  /// cleared from the data store.
  /// </summary>
  [Fact]
  public void LogginInAfterPasswordResetOperationClearsResetData()
  {

    const string USER_NAME = nameof(LogginInAfterPasswordResetOperationClearsResetData) + "@test.com";
    const string EMAIL = USER_NAME;
    const string PASS = "ABC";

    SignupAndVerifyNewUser(USER_NAME, EMAIL, PASS, out TestContext? context);

    // Use the reset feature + parse the email for the reset URL / code.
    var response = context.LoginCtl.ForgotPassword(new ForgotPasswordArgs()
    {
      Username = USER_NAME
    });
    Assert.True(response.Code == 0, "Invalid response code!");

    // Make sure we are not logged in....
    {
      var vr = context.LoginCtl.ValidateLogin();
      Assert.False(vr.IsLoggedIn, "The user should not be logged in!");
    }

    // Login + check the password reset data in the DB.
    {
      var vr = context.LoginCtl.Login(new LoginModel()
      {
        username = USER_NAME,
        password = PASS,
      });
      Assert.True(vr.IsLoggedIn, "We should be logged in now!");
    }

    // Make sure that there isn't any reset data in the DB after logging in....
    Member? m = context.LoginCtl.DAL.GetMember(USER_NAME);
    Assert.NotNull(m);
    Assert.Null(m.ResetToken);
    Assert.Null(m.TokenExpires);

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
    string verifyCode = href.Split("?resetToken=")[1];    // <-- We should probably parse the url + extract querystring properly.
    return verifyCode;
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
    // Remove the test user.
    CleanupTestUser(username);

    context = CreateTestContext();

    SignupResponse response = context.LoginCtl.Signup(new LoginModel()
    {
      username = username,
      email = email,
      password = password
    });
    Assert.Equal(0, response.Code);

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private TestContext CreateTestContext()
  {
    var appBuilder = WebApplication.CreateBuilder();
    var cfgHelper = Program.InitConfig(appBuilder, appBuilder.Environment);

    // Create the login controller.
    // Signup the user + validate availability.
    var emailSvc = GetEmailService();

    // NOTE: The membershiphelpers are all using the same path for the active login data.
    // We should maybe find a way to isolate those?
    var mmHelper = GetMembershipHelper(cfgHelper);
    var loginCtl = new SimLoginController(GetMemberAccess(), emailSvc, cfgHelper, mmHelper);
    var context = new TestContext(cfgHelper, emailSvc, loginCtl, mmHelper);

    return context;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private SimMembershipHelper GetMembershipHelper(ConfigHelper cfgHelper)
  {
    var cfg = cfgHelper.Get<MemberManConfig>();
    var res = new SimMembershipHelper(cfg);
    res.LoadActiveUserList(DateTimeOffset.Now);

    return res;
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