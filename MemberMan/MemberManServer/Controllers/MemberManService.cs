using drewCo.Tools;
using officepark.io.Membership;
using Org.BouncyCastle.Bcpg.Sig;

namespace MemberMan;

// ==============================================================================================================================
public class MemberManService
{
  public static class ServiceCodes
  {
    public const int INVALID_VERIFICATION = 0x11;
    public const int VERIFICATION_EXPIRED = 0x12;
    public const int NOT_VERFIED = 0x13;
    public const int LOGIN_FAILED = 0x14;
    public const int INVALID_RESET_TOKEN = 0x15;
    public const int RESET_TOKEN_EXPIRED = 0x16;

    /// <summary>
    /// The user is already logged in.
    /// </summary>
    public const int LOGGED_IN = 0x17;
  }

  public IMemberAccess DAL = null!;
//  public MemberManConfig MemberManConfig = null!;
  public MembershipHelper MemberHelper = null!;
  private IEmailService Email = null!;

  // --------------------------------------------------------------------------------------------------------------------------
  public MemberManService(IMemberAccess dal_, MembershipHelper memberHelper_, IEmailService email_)
  {
    DAL = dal_;
    MemberHelper = memberHelper_;
    Email = email_;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public Member? Login(LoginModel login)
  {
    // Reach into the DAL to look for active user + password.
    Member? m = DAL.GetMember(login.username, login.password);
    if (m != null)
    {
      DAL.RemovePasswordResetData(m.Username);
    }
    return m;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // TODO: This could also go with some kind of base class?
  public bool IsLoggedIn(string loginToken, string ipAddress)
  {
    bool res = MemberHelper.IsLoggedIn(loginToken, ipAddress);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public bool IsLoggedIn(string loginToken, string ipAddress, out Member? m)
  {
    m = GetLoggedInMember(loginToken, ipAddress);
    return m != null;
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public string GeneratePasswordResetToken()
  {
    // TODO: Some kind of crypto / random hash or something?
    // NOTE: This should be a plugin type function so that users may define their own algos.....
    var uuid = Guid.NewGuid().ToString();
    string res = StringTools.ComputeSHA1(uuid);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetLoggedInMember(string? memberCookie, string ipAddress)
  {
    string? token = MembershipHelper.GetLoginToken(memberCookie, ipAddress);
    var res = MemberHelper.GetMember(token);
    return res;
  }


}
