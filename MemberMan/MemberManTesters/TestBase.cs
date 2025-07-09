using System.IO;
using drewCo.Tools;
using officepark.io.Membership;

// ==========================================================================
public class TestBase
{

  // --------------------------------------------------------------------------------------------------------------------------
  protected void CleanupTestUser(string username)
  {
    IMemberAccess dal = GetMemberAccess();
    dal.RemoveMember(username, false);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected IMemberAccess GetMemberAccess()
  {
    string testDir = Path.Combine(FileTools.GetAppDir(), "TestData");
    FileTools.CreateDirectory(testDir);

    // NOTE: We have nothing in here for username/password, and we really should for security purposes.
    var pwHandler = new TestPasswordHandler();
    var pwValidator = new TestPasswordValidator();  

    var res = new SqliteMemberAccess(testDir, "MemberMan", pwHandler, pwValidator); 
    if (!File.Exists(res.DBFilePath))
    {
      res.SetupDatabase();
    }

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected SimEmailService GetEmailService()
  {
    return new SimEmailService();
  }


}


// ============================================================================================================================
public class TestPasswordValidator : IPasswordValidator
{
  // --------------------------------------------------------------------------------------------------------------------------
  public PasswordValidationResult Validate(string password)
  {
    bool ok = !string.IsNullOrWhiteSpace(password);
    return new PasswordValidationResult(ok, ok ? null : "The password cannot be empty!");  
  }
}

// ============================================================================================================================
/// <summary>
/// This passeword handler is used in test cases so we don't have to wait around for bcrypt and others
/// to do the long work of checking/generating passwords.
/// </summary>
public class TestPasswordHandler : IPasswordHandler
{
  // --------------------------------------------------------------------------------------------------------------------------
  public bool CheckPassword(string password, string hash)
  {
    bool res = password == hash;
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public string GetPasswordHash(string password)
  {
    string res = password;
    return res;
  }
}
