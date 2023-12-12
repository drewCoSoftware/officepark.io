
using MemberMan;
using Org.BouncyCastle.Asn1;
using BC = BCrypt.Net.BCrypt;


namespace officepark.io.Membership;

// ==========================================================================
public record MemberAvailability(bool IsUsernameAvailable, bool IsEmailAvailable);

// ==========================================================================
public interface IMemberAccess
{
  // OPTIONS / PRE-PROCESSOER:
  public const int DEFAULT_WORK_FACTOR = 15;

  MemberAvailability CheckAvailability(string username, string email);

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Check a user's login credentials.
  /// </summary>
  Member? GetMember(string username, string password);

  // --------------------------------------------------------------------------------------------------------------------------
  // Cool.  With default implementations, we complete the loop and have ABCs! Lol, j/k.
  string GetPasswordHash(string password, int workFactor = DEFAULT_WORK_FACTOR)
  {
    string hashed = BC.HashPassword(password, workFactor);
    return hashed;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Compares the given password to the hash.  This will tell us if the password is actually correct or not.
  /// </summary>
  /// TODO: We should allow plugins for different pasword checking schemes...
  bool CheckPassword(string password, string hash)
  {
    bool res = BC.Verify(password, hash);
    return res;
  }

  Member? GetMember(string username);
  List<Member> GetMemberList();

  Member CreateMember(string username, string email, string password, TimeSpan verifyWindow);
  void RemoveMember(string username, bool mustExist = true);

  /// <summary>
  /// Change the verification code and expiration date.
  /// </summary>
  Member RefreshVerification(string username, TimeSpan verifyWindow);

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
  void SetPasswordResetData(string username, string resetToken, DateTimeOffset? tokenExpires);
  void RemovePasswordResetData(string username);
  Member GetMemberByResetToken(string resetToken);
  void SetPassword(string username, string newPassword);
}

