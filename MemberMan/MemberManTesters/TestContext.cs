using MemberMan;
using MemberManTesters.SimTypes;

namespace MemberManTesters;

public partial class ServiceTesters
{
  // ============================================================================================================================
  class TestContext
  {
    // --------------------------------------------------------------------------------------------------------------------------
    internal TestContext(ConfigHelper config_, SimEmailService emailSvc_, LoginApiController loginCtl_, SimMembershipHelper mmHelper_)
    {
      Config = config_;
      EmailSvc = emailSvc_;
      LoginCtl = loginCtl_;
      MembershipHelper = mmHelper_;
    }

    public ConfigHelper Config { get; private set; } = null!;
    public SimEmailService EmailSvc { get; private set; } = null!;
    public LoginApiController LoginCtl { get; private set; } = null!;
    public SimMembershipHelper MembershipHelper { get; set; } = null!;

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Resets the state of the context, last emails sent and whatnot....
    /// </summary>
    /// NOTE: Not really sure what to name this thing.....
    internal void NextRequest()
    {
      EmailSvc.ClearLastEmail();
    }
  }
}