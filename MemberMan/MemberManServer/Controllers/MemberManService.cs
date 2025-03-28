using drewCo.Tools;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using officepark.io.API;
using officepark.io.Membership;
using Org.BouncyCastle.Bcpg.Sig;

namespace MemberMan;

// ==============================================================================================================================
public class ResetPasswordResponse
{
  public int Code { get; set; }
  public string Message { get; set; }
}

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
  // NOTE: A cool dhll features could be 'proxy' keyword where we offer up functions that are forwarded to a certain member class / function.
  public bool HasPermission(Member m, string? requiredPermissions)
  {
    return MemberHelper.HasPermission(m, requiredPermissions);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public bool IsLoggedIn(HttpRequest req) {
    return MemberHelper.IsLoggedIn(req);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public void UpdateLoginCookie(HttpRequest req, HttpResponse res)
  {
    MemberHelper.UpdateLoginCookie(req, res);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetMemberByRequest(HttpRequest req)
  {
    var res = MemberHelper.GetMember(req);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member? GetMemberByName(string username)
  {
    return DAL.GetMember(username);
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public Member GetMemberByToken(string loginToken)
  {
    return MemberHelper.GetMember(loginToken);
  }


  // --------------------------------------------------------------------------------------------------------------------------
  public ResetPasswordResponse ResetPassword(ResetPasswordArgs args)
  {
    if (args.NewPassword != args.ConfirmPassword)
    {
      return new ResetPasswordResponse()
      {
        Code = -1,
        Message = "Passwords do not match!"
      };
    }

    // Get the member with the given token.
    var member = DAL.GetMemberByResetToken(args.ResetToken);
    if (member == null || member.TokenExpires == null || member.ResetToken == null)
    {
      return new ResetPasswordResponse()
      {
        Code = MemberManService.ServiceCodes.INVALID_RESET_TOKEN,
        Message = "Invalid reset token!"
      };
    }

    var timestamp = DateTimeOffset.Now;
    int code = 0;
    string msg = string.Empty;
    if (member.ResetToken != args.ResetToken)
    {
      code = MemberManService.ServiceCodes.INVALID_RESET_TOKEN;
      msg = "Reset token mismatch!";
    }

    if (timestamp > member.TokenExpires)
    {
      code = MemberManService.ServiceCodes.RESET_TOKEN_EXPIRED;
      msg = "Reset token expired!";
    }

    if (code == 0)
    {
      DAL.RemovePasswordResetData(member.Username);
      DAL.SetPassword(member.Username, args.NewPassword);
    }

    return new ResetPasswordResponse()
    {
      Code = code,
      Message = msg
    };

  }

  //// --------------------------------------------------------------------------------------------------------------------------
  //public Member GetMemberByResetToken(string resetToken)
  //{
  //  var res = DAL.GetMemberByResetToken(resetToken);
  //  return res;
  //}

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

  // --------------------------------------------------------------------------------------------------------------------------
  internal string BeginPasswordReset(string username)
  {
    // If they exist, then we will generate a reset token/code.
    // The DB must be updated at this point.
    string res = GeneratePasswordResetToken();
    DateTimeOffset tokenExpires = DateTimeOffset.UtcNow + MemberHelper.Config.PasswordResetWindow;
    DAL.SetPasswordResetData(username, res, tokenExpires);

    return res;

  }
}
