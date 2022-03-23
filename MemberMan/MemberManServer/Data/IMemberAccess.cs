
namespace officepark.io.Membership;


// ==========================================================================
public interface IMemberAccess
{
  Member? CheckLogin(string username, string password);

  // --------------------------------------------------------------------------------------------------------------------------
  // Cool.  With default implementations, we complete the loop and have ABCs! Lol, j/k.
  string GetPasswordHash(string password)
  {
    // OPTIONS / PRE-PROCESSOER:
    const int WORK_FACTOR = 15;

    string hashed = BCrypt.Net.BCrypt.HashPassword(password, WORK_FACTOR);
    return hashed;
  }

  Member? GetMemberByName(string name);
  List<Member> GetMemberList();

  Member CreateMember(string username, string password);

  /// <summary>
  /// A function to verify that the given password matches the matching password.
  /// The matching password -> match algorithm can be anything you like, but
  /// is plaintext -> bcrypt hash by default.
  /// </summary>
  bool VerifyPassword(string password, string match)
  {
    bool res = BCrypt.Net.BCrypt.Verify(password, match);
    return res;
  }
}

