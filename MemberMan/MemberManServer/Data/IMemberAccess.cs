
namespace officepark.io.Membership;

// ==========================================================================
public record MemberAvailability(bool IsUsernameAvailable, bool IsEmailAvailable);

// ==========================================================================
public interface IMemberAccess
{
  // OPTIONS / PRE-PROCESSOER:
  public const int DEFAULT_WORK_FACTOR = 15;

  MemberAvailability CheckAvailability(string username, string email);

  /// <summary>
  /// Check a user's login credentials.
  /// </summary>
  Member? CheckLogin(string username, string password);

  // --------------------------------------------------------------------------------------------------------------------------
  // Cool.  With default implementations, we complete the loop and have ABCs! Lol, j/k.
  string GetPasswordHash(string password, int workFactor = DEFAULT_WORK_FACTOR)
  {
    string hashed = BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    return hashed;
  }

  Member? GetMemberByName(string username);
  List<Member> GetMemberList();

  Member CreateMember(string username, string email, string password);
  void RemoveMember(string username);

  /// <summary>
  /// Change the verification code and expiration date.
  /// </summary>
  Member RefreshVerification(string username);

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

  Member? GetMemberByVerification(string verificationCode);
  void CompleteVerification(Member member, DateTimeOffset date);
}

