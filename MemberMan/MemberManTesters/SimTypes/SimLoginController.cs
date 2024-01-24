

using MemberMan;
using officepark.io.Membership;

// ==========================================================================
public class SimLoginController : LoginController
{
  // --------------------------------------------------------------------------------------------------------------------------
  public SimLoginController(IMemberAccess dal_, IEmailService email_, ConfigHelper config_, MembershipHelper mmHelper_)
   : base(dal_, email_, config_, mmHelper_)
  {
    _IPAddress = "127.0.0.1";
    _LoginToken = null; // "abc-def";
    MembershipCookie = null;
  }

  #region Properties 

  public override string? MembershipCookie { get; internal set; } = null;


  // --------------------------------------------------------------------------------------------------------------------------
  protected override string GetTemplateText(string templateName)
  {
    if (templateName == EmailTemplateNames.FORGOT_PASSWORD_TEMPLATE)
    {
      return "{{model.ResetLink}}";
    }
    else
    {
      return base.GetTemplateText(templateName);
    }
  }
  // public override string IPAddress { get; protected set; } = "127.0.0.1";

  #endregion
}