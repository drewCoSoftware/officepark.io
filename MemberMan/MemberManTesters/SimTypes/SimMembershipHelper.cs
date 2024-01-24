using MemberMan;
using officepark.io.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemberManTesters.SimTypes
{
  // ============================================================================================================================
  internal class SimMembershipHelper : MembershipHelper
  {
    // --------------------------------------------------------------------------------------------------------------------------
    public SimMembershipHelper(MemberManConfig config_)
      : base(config_)
    { }

    // --------------------------------------------------------------------------------------------------------------------------
    public bool IsUserActive(string username)
    {
      var match = (from x in LoggedInMembers
                   where x.Value.Username == username
                   select x.Value).FirstOrDefault();

      bool res = match != null;
      return res;
    }
  }
}
