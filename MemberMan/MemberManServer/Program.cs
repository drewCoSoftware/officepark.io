using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using CommandLine;
using DotLiquid;
using DotLiquid.Util;
using drewCo.Tools;
using MemberMan;
using MemberManServer;
using officepark.io.Membership;
using static MemberManServer.Mailer;

// ============================================================================================================================
internal class Program
{
  static SqliteMemberAccess MemberAccess = default!;


  // --------------------------------------------------------------------------------------------------------------------------
  private static int Main(string[] args)
  {

    if (HandleCommandLine(args, out int exitCode))
    {
      return exitCode;
    }



    var builder = WebApplication.CreateBuilder(args);

    var cfgHelper = InitConfig(builder, builder.Environment);
    var mmCfg = cfgHelper.Get<MemberManConfig>();

    MemberAccess = InitMMDatabase();

    builder.Services.AddSingleton<IMemberAccess>(MemberAccess);

    EmailService email = new EmailService(mmCfg.EmailConfig);
    builder.Services.AddSingleton((IEmailService)email);

    var mmHelper = new MembershipHelper(mmCfg);
    mmHelper.LoadActiveUserList(DateTimeOffset.Now);
    builder.Services.AddSingleton<MembershipHelper>(mmHelper);  

    var mmService = new MemberManService(MemberAccess, mmHelper, email);
    builder.Services.AddSingleton(mmService);



    var ctl = builder.Services.AddControllers();
    ctl.AddJsonOptions((ops) =>
    {
      ops.JsonSerializerOptions.PropertyNamingPolicy = null;

      // var enumConverter = new JsonStringEnumConverter();
      // ops.JsonSerializerOptions.Converters.Add(enumConverter);
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddCors((ops) =>
    {
      ops.AddPolicy("corsPolicy", builder =>
      {
        builder.SetIsOriginAllowed(IsOriginAllowed);
        builder.AllowCredentials();
        builder.WithHeaders("content-type");

        // TODO: Use environment variable for this.
#if DEBUG
        builder.WithHeaders("x-test-api-call");
#endif
      });
    });




    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.UseCors("corsPolicy");


    // Error Handler / Logger:
    // Arguments can be added to the constructor here so that configurations can be made as needed...
    app.UseMiddleware(typeof(ErrorHandler));


    app.Run();


    return 0;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  internal static ConfigHelper InitConfig(WebApplicationBuilder builder, IHostEnvironment env)
  {
    ConfigurationManager cfg = builder.Configuration;
    cfg.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", false, true)
       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
       .AddJsonFile("appsettings.local.json", true, true);

    var helper = new ConfigHelper(cfg);
    builder.Services.AddSingleton(helper);


    return helper;

    // Leave this around for examples.
    //private static IConfiguration BuildConfiguration(IHostEnvironment env)
    //{
    //  var configurationBuilder = new ConfigurationBuilder()
    //      .SetBasePath(Directory.GetCurrentDirectory())
    //      .AddJsonFile("./Configuration/appsettings.json", optional: false, reloadOnChange: true)
    //      .AddJsonFile("./Configuration/appsettings.other.json", optional: false, reloadOnChange: true)
    //      .AddJsonFile($"./Configuration/appsettings.{env.EnvironmentName}.json", optional: true)
    //      .AddJsonFile($"./Configuration/appsettings.other.{env.EnvironmentName}.json", optional: true)
    //  .AddEnvironmentVariables();

    //  Configuration = configurationBuilder.Build();
    //  return Configuration;
    //}
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static int ListUsers(ListUsersOptions options)
  {
    InitMMDatabase(options.DatabaseFile);
    List<Member> members = MemberAccess.GetMemberList();

    Console.WriteLine("USERNAME\t|\tEMAIL");
    Console.WriteLine("------------------------------------");
    foreach(var m in members) 
    {
      Console.WriteLine($"{m.Username}\t|\t{m.Email}");
    }

    return 0;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static int DeleteUser(DeleteUserOptions options)
  {
    try
    {
      InitMMDatabase(options.DatabaseFile);
      MemberAccess.RemoveMember(options.Username!, true);
    }
    catch (Exception ex)
    {
      Console.WriteLine("Delete user failed!");
      Console.WriteLine(ex.Message);
      return -1;

    }
    return 0;

  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static int CreateUser(CreateUserOptions options)
  {
    try
    {
      InitMMDatabase(options.DatabaseFile);

      string username = string.IsNullOrWhiteSpace(options.Username) ? options.Email : options.Username;
      Member m = MemberAccess.CreateMember(username, options.Email, options.Password, MemberManConfig.DEFAULT_VERIFY_WINDOW);

      if (!string.IsNullOrWhiteSpace(options.Permissions))
      {
        m.Permissions = options.Permissions;
      }

      m.VerifiedOn = DateTimeOffset.Now;
      MemberAccess.UpdateMember(m);

    }
    catch (Exception ex)
    {
      Console.WriteLine("Create user failed!");
      Console.WriteLine(ex.Message);
      return -1;
    }

    return 0;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private static bool HandleCommandLine(string[] args, out int exitCode)
  {
    exitCode = 0;
    if (args.Length > 0)
    {
      exitCode = Parser.Default.ParseArguments<CreateUserOptions, DeleteUserOptions, ListUsersOptions>(args)
                       .MapResult((CreateUserOptions ops) => CreateUser(ops),
                                  (DeleteUserOptions ops) => DeleteUser(ops),
                                  (ListUsersOptions ops) => ListUsers(ops),
                       err => 1);
      return true;
    }
    else
    {
      return false;
    }

    // try
    // {
    //   // TODO: Proper commandline args?
    //   // Care about validation? --> Not at this time!
    //   // NOTE: A more secure scenario would require us to use a specific DB login/other creds
    //   // for this kind of thing.

    //   if (args.Length > 0 && args[0] == "delete-user")
    //   {
    //     string username = args[1];
    //     Console.WriteLine($"Deleting the user: {username}!");

    //     MemberAccess.RemoveMember(username);
    //     Console.WriteLine($"DELETED!");

    //     return true;
    //   }

    //   if (args.Length > 0 && args[0] == "create-admin")
    //   {
    //     Console.WriteLine("Creating admin user for database!");

    //     string username = args[1];
    //     string email = args[2];
    //     string password = args[3];

    //     Member m = MemberAccess.CreateMember(username, email, password, MemberManConfig.DEFAULT_VERIFY_WINDOW);

    //     m.Permissions = "ADMIN";
    //     m.VerificationCode = "cli-created";
    //     m.VerifiedOn = DateTimeOffset.Now;

    //     MemberAccess.UpdateMember(m);

    //     return true;
    //   }

    //   // ..... Other CLI stuff?

    //   return false;
    // }
    // catch (Exception ex)
    // {
    //   // TODO: Log exception?
    //   Console.WriteLine(ex.Message);
    //   exitCode = 1;
    // }

    // // No special command line stuff was used.
    // return false;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  // REFACTOR: This can be used in other projects and should be refactored into a lib??
  static bool IsOriginAllowed(string host)
  {
    // NOTE: These could come from some kind of updateable cache / settings file.
    var corsOriginAllowed = new[] { "localhost" };

    return corsOriginAllowed.Any(origin =>
        Regex.IsMatch(host, $@"^http(s)?://.*{origin}(:[0-9]+)?$", RegexOptions.IgnoreCase));
  }

  private static bool IsDBInitialized = false;
  const string DEFAULT_DATA_DIR = "data";
  const string DEFAULT_DB_FILE_NAME = "member-man";

  // --------------------------------------------------------------------------------------------------------------------------
  private static SqliteMemberAccess InitMMDatabase(string? dbFilePath = null)
  {
    if (!IsDBInitialized)
    {
      // NOTE: We may even want to extract these from some environment variables?
      string dir = DEFAULT_DATA_DIR;
      string filename = DEFAULT_DB_FILE_NAME;
      if (!string.IsNullOrEmpty(dbFilePath))
      {
        dir = Path.GetDirectoryName(dbFilePath)!;
        filename = Path.GetFileName(dbFilePath);
        filename = StringTools.TrimEnd(filename, ".sqlite");
      }

      FileTools.CreateDirectory(dir);

      MemberAccess = new SqliteMemberAccess(dir, filename);
      if (!File.Exists(MemberAccess.DBFilePath))
      {
        MemberAccess.SetupDatabase();
      }
      IsDBInitialized = true;
    }
    else
    {
      Console.WriteLine("The database has already been initialized!");
    }

    return MemberAccess;
  }
}