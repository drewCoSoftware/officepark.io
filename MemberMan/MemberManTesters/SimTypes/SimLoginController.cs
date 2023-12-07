

using MemberMan;
using officepark.io.Membership;

// ==========================================================================
public class SimLoginController : LoginController
{
  // --------------------------------------------------------------------------------------------------------------------------
  public SimLoginController(IMemberAccess dal_, IEmailService email_, ConfigHelper config_)
   : base(dal_, email_, config_)
  { 
      _IPAddress = "127.0.0.1";
      _LoginToken = "abc-def";
  }

    #region Properties 

   // public override string IPAddress { get; protected set; } = "127.0.0.1";

    #endregion
}