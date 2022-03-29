

using System;
using Xunit;

public class ServiceTesters
{
  // --------------------------------------------------------------------------------------------------------------------------
  [Fact]
  public void CanSignupAndVerifyNewUser()
  {
    const string NAME = nameof(CanSignupAndVerifyNewUser);
    const string EMAIL = NAME + "@test.com";
    const string PASS = "ABC";

    // Remove the test user.
    // Create the login controller.
    // Signup the user + validate availability.
    // Confirm that an email was sent + get its content.

    // Visit the verification URL (from the email)
    // Confirm that the user is now verified in the DB.

    throw new NotImplementedException("Please finish this test!");
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