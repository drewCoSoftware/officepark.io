using System;
using System.IO;
using drewCo.Tools;
using Microsoft.Data.Sqlite;
using officepark.io.Data;
using officepark.io.Membership;
using Xunit;

namespace MemberManTesters
{
  public class DataTesters
  {
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

      const string EXPECTED = "INSERT INTO Members (username,email,membersince,permissions,password) VALUES (@Username,@Email,@MemberSince,@Permissions,@Password) RETURNING id";
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
      dal.CreateMember(USER1, EMAIL1, "abc");
      Assert.Throws<SqliteException>(() => {
        dal.CreateMember(USER1, EMAIL1, "abc");
      });

      // This email address has already been used!
      Assert.Throws<SqliteException>(() => {
        dal.CreateMember(USER2, EMAIL1, "abc");
      });

      // New user + email combination will be OK!
      Member m = dal.CreateMember(USER2, EMAIL2, "abc");
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

      Member src = dal.CreateMember(TEST_USER, TEST_EMAIL, TEST_PASS);
      Assert.NotNull(src);

      Member comp = dal.GetMemberByName(TEST_USER)!;
      Assert.NotNull(comp);

      Assert.Equal(src.MemberSince, comp.MemberSince);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void CleanupTestUser(string username)
    {
      IMemberAccess dal = GetMemberAccess();
      dal.RemoveMember(username);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IMemberAccess GetMemberAccess()
    {
      string testDir = Path.Combine(FileTools.GetAppDir(), "TestData");
      FileTools.CreateDirectory(testDir);

      // NOTE: We have nothing in here for username/password, and we really should for security purposes.
      var res = new SqliteMemberAccess(testDir, "MemberMan"); //   FileSystemMemberAccess();
      if (!File.Exists(res.DBFilePath))
      {
        res.SetupDatabase();
      }

      return res;
    }
  }
}