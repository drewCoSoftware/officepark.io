
using MemberMan;
using Org.BouncyCastle.Asn1;
using BC = BCrypt.Net.BCrypt;


namespace officepark.io.Membership;

public class PermissionResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// ==========================================================================
public interface IMemberAccess
{

    MemberAvailability CheckAvailability(string username, string email);
    IPasswordHandler PasswordHandler { get; }
    IPasswordValidator PasswordValidator { get; }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Add a permission to the member's permission list.
    /// This should only be used on members that have full trust / during initial setup.
    /// </summary>
    PermissionResult AddPermission(Member toMember, string permission);

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// One member grants permission to another given that they have 'grantCode' set in their permissions list.
    /// </summary>
    /// <param name="grantor"></param>
    /// <param name="grantee"></param>
    /// <param name="permission"></param>
    /// <param name="grantCode"></param>
    /// <returns></returns>
    PermissionResult GrantPermission(Member grantor, Member grantee, string permission, string grantCode);

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Check a user's login credentials.
    /// </summary>
    Member? GetMember(string username, string password);

    // --------------------------------------------------------------------------------------------------------------------------
    // Cool.  With default implementations, we complete the loop and have ABCs! Lol, j/k.
    string GetPasswordHash(string password)
    {
        return PasswordHandler.GetPasswordHash(password);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Compares the given password to the hash.  This will tell us if the password is actually correct or not.
    /// </summary>
    /// TODO: We should allow plugins for different pasword checking schemes...
    bool CheckPassword(string password, string hash)
    {
        return PasswordHandler.CheckPassword(password, hash);
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

    bool HasPermission(string username, string? requiredPermissions);
}



// ==========================================================================
public record MemberAvailability(bool IsUsernameAvailable, bool IsEmailAvailable);

// ============================================================================================================================
public interface IPasswordHandler
{
  string GetPasswordHash(string password);
  bool CheckPassword(string password, string hash);
}

// ============================================================================================================================
/// <summary>
/// Password validators are responsible for making sure that the given password format is correct.
/// Length, characters, etc. 
/// </summary>
public interface IPasswordValidator
{
  PasswordValidationResult Validate(string password);
}

// ============================================================================================================================
/// <param name="IsValid"></param>
/// <param name="Message">Custom message to describe any issues with the password if it is not valid, etc.</param>
public record class PasswordValidationResult(bool IsValid, string? Message = null);

// ============================================================================================================================
public class DefaultPasswordValidator : IPasswordValidator
{
  public const int MIN_LENGTH = 8;

  // --------------------------------------------------------------------------------------------------------------------------
  public PasswordValidationResult Validate(string password)
  {
    var reasons = new List<string>();

    if (string.IsNullOrWhiteSpace(password))
    {
      reasons.Add("The password may not be empty!");
    }
    else
    {
      int len = password.Length;
      if (len < MIN_LENGTH)
      {
        reasons.Add($"The password must be at least {MIN_LENGTH} characters!");
      }

    }


    // Validation steps complete, return some results....
    if (reasons.Count == 0)
    {
      return new PasswordValidationResult(true);
    }
    else
    {
      string msg = string.Join(Environment.NewLine, reasons);
      return new PasswordValidationResult(false, msg);
    }

  }
}

// ============================================================================================================================
public class BCryptPasswordHandler : IPasswordHandler
{
  // OPTIONS / PRE-PROCESSOER:
  public const int DEFAULT_WORK_FACTOR = 15;

  // --------------------------------------------------------------------------------------------------------------------------
  // Cool.  With default implementations, we complete the loop and have ABCs! Lol, j/k.
  public string GetPasswordHash(string password)
  {
    string hashed = BC.HashPassword(password, DEFAULT_WORK_FACTOR);
    return hashed;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Compares the given password to the hash.  This will tell us if the password is actually correct or not.
  /// </summary>
  /// TODO: We should allow plugins for different pasword checking schemes...
  public bool CheckPassword(string password, string hash)
  {
    bool res = BC.Verify(password, hash);
    return res;
  }
}

