using System;
using System.IO;
using DataHelpers.Data;
using drewCo.Tools;
using MemberMan;
using Microsoft.Data.Sqlite;
using officepark.io.Membership;
using Xunit;

namespace MemberManTesters
{

  // ==========================================================================
  public class DataTesters : TestBase
  {
    // -------------------------------------------------------------------------------------------------------------------------- 
    [Fact]
    public void CanCheckUsernameAvailability()
    {
      const string TEST_USER = "DavidDavis";
      const string TEST_EMAIL = "dave@davis.com";
      CleanupTestUser(TEST_USER);

      var dal = GetMemberAccess();
      {
        MemberAvailability exists = dal.CheckAvailability(TEST_USER, TEST_EMAIL);
        Assert.True(exists.IsUsernameAvailable && exists.IsEmailAvailable);
      }

      // Create a new user with name / email.
      dal.CreateMember(TEST_USER, TEST_EMAIL, "abc", MemberManConfig.DEFAULT_VERIFY_WINDOW);
      {
        MemberAvailability exists = dal.CheckAvailability(TEST_USER, TEST_EMAIL);
        Assert.False(exists.IsUsernameAvailable);
        Assert.False(exists.IsEmailAvailable);
      }

    }

    // -------------------------------------------------------------------------------------------------------------------------- 
    /// <summary>
    /// Shows that we can automatically create an insert query for a table.
    /// </summary>
    [Fact]
    public void CanCreateInsertQuery()
    {
      var schema = new SchemaDefinition(new SqliteFlavor(), typeof(MemberManSchema));
      TableDef memberTable = schema.TableDefs[0];

      string insertQuery = memberTable.GetInsertQuery();

      const string EXPECTED = "INSERT INTO Members (username,email,createdon,verificationcode,verificationexpiration,verifiedon,permissions,password) VALUES (@Username,@Email,@CreatedOn,@VerificationCode,@VerificationExpiration,@VerifiedOn,@Permissions,@Password) RETURNING id";
      Assert.Equal(EXPECTED, insertQuery);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Shows that the username and email fields must be unique when adding users.
    /// </summary>
    [Fact]
    public void CantDoubleAddUserByNameOrEmail()
    {
      const string USER1 = "USER 1";
      const string USER2 = "USER 2";
      const string EMAIL1 = "EMAIL1@EMAIL.COM";
      const string EMAIL2 = "EMAIL2@EMAIL.COM";

      CleanupTestUser(USER1);
      CleanupTestUser(USER2);

      IMemberAccess dal = GetMemberAccess();
      dal.CreateMember(USER1, EMAIL1, "abc", MemberManConfig.DEFAULT_VERIFY_WINDOW);
      Assert.Throws<SqliteException>(() =>
      {
        dal.CreateMember(USER1, EMAIL1, "abc", MemberManConfig.DEFAULT_VERIFY_WINDOW);
      });

      // This email address has already been used!
      Assert.Throws<SqliteException>(() =>
      {
        dal.CreateMember(USER2, EMAIL1, "abc", MemberManConfig.DEFAULT_VERIFY_WINDOW);
      });

      // New user + email combination will be OK!
      Member m = dal.CreateMember(USER2, EMAIL2, "abc", MemberManConfig.DEFAULT_VERIFY_WINDOW);
      Assert.NotEqual(0, m.ID);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CanCreateMember()
    {
      IMemberAccess dal = GetMemberAccess();

      const string TEST_USER = nameof(CanCreateMember) + "_MEMBER";
      CleanupTestUser(TEST_USER);

      const string TEST_PASS = "123";
      const string TEST_EMAIL = "abc@123.com";

      Member src = dal.CreateMember(TEST_USER, TEST_EMAIL, TEST_PASS, MemberManConfig.DEFAULT_VERIFY_WINDOW);
      Assert.NotNull(src);
      Assert.NotNull(src.VerificationCode);

      Member comp = dal.GetMemberByName(TEST_USER)!;
      Assert.NotNull(comp);

      Assert.Equal(src.CreatedOn, comp.CreatedOn);
      Assert.False(comp.IsVerified);
    }


  }
}