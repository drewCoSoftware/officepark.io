
// ============================================================================================================================
/// <summary>
/// Makes it easy to bind data out of the application configuration.
/// </summary>
public class ConfigHelper
{
  private IConfiguration Config = null!;

  // --------------------------------------------------------------------------------------------------------------------------
  public ConfigHelper(IConfiguration cfg_)
  {
    Config = cfg_;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public T Get<T>(string? key = null)
  {
    if (key == null)
    {
      key = typeof(T).Name;
    }

    // NOTE: We could improve the performance of this by caching the results for a given key.
    // We would need to rebind it if the source files changed however and I'm not sure how
    // I would make that work.....
    // HINT: -->     Config.GetReloadToken
    T res = Activator.CreateInstance<T>();
    Config.Bind(key, res);


    return res;
  }

}
