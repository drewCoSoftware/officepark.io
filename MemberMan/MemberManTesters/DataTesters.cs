using System;
using System.IO;
using drewCo.Tools;
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
        var schema  = new SchemaDefinition(new SqliteFlavor(), typeof(MemberManSchema));
        TableDef memberTable = schema.TableDefs[0];

        string insertQuery = memberTable.GetInsertQuery();

        const string EXPECTED = "INSERT INTO Members (username,email,membersince,permissions,password) VALUES (@Username,@Email,@MemberSince,@Permissions,@Password) RETURNING id";
        Assert.Equal(EXPECTED, insertQuery);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CanCreateMember()
    {
      IMemberAccess dal = GetMemberAccess();

      const string TEST_USER = nameof(CanCreateMember) + "_MEMBER";
      const string TEST_PASS = "123";

      Member src = dal.CreateMember(TEST_USER, TEST_PASS);
      Assert.NotNull(src);

      Member comp = dal.GetMemberByName(TEST_USER)!;
      Assert.NotNull(comp);

      Assert.Equal(src.MemberSince, comp.MemberSince);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IMemberAccess GetMemberAccess()
    {
      string testDir = Path.Combine(FileTools.GetAppDir(), "TestData");
      FileTools.CreateDirectory(testDir);

      var res = new SqliteMemberAccess(testDir, "MemberMan"); //   FileSystemMemberAccess();
      if (!File.Exists(res.DBFilePath))
      {
        res.SetupDatabase();
      }

      return res;
    }
  }
}