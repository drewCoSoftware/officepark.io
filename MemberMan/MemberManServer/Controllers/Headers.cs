namespace MemberMan;

// ============================================================================================================================
public static class Headers
{
  public const string MM_TEST_MODE = "X-MMTestMode";

  /// <summary>
  /// Indicates the type of test that we are running.
  /// If not set, we should assume that the test passes.
  /// See the <see cref="TestTypes"/> for example values, but note that the value can be anything that
  /// is required to test a specific scenario.
  /// </summary>
  public const string MM_TEST_TYPE = "X-MMTestType";

  // ============================================================================================================================
  public static class TestTypes
  {
    public const string TEST_PASS = "Pass";
    public const string TEST_FAIL = "Fail";
  }

}
