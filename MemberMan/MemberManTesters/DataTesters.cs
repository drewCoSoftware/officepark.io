using officepark.io.Membership;
using Xunit;

namespace MemberManTesters
{
  public class DataTesters
  {
    // --------------------------------------------------------------------------------------------------------------------------
    [Fact]
    public void CanCreateMember()
    {
      IMemberAccess dal = new FileSystemMemberAccess();

      const string TEST_USER = nameof(CanCreateMember) + "_MEMBER";
      const string TEST_PASS = "123";

      Member src = dal.CreateMember(TEST_USER, TEST_PASS);
      Assert.NotNull(src);

      Member comp = dal.GetMemberByName(TEST_USER);
      Assert.NotNull(comp);

      Assert.Equal(src.MemberSince, comp.MemberSince);
    }
  }
}