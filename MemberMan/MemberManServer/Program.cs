using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using drewCo.Tools;
using MemberManServer;
using officepark.io.Membership;
using static MemberManServer.Mailer;

internal class Program
{
  static SqliteMemberAccess memberAccess = default!;

  // OPTIONS:
  const string EMAIL_FROM = "info@august-harper.com";
  const string SMTP_SERVER = "mx-s3.vivawebhost.com";
  const int SMTP_PORT = 465;


  // --------------------------------------------------------------------------------------------------------------------------
  private static int Main(string[] args)
  {
    // TEMP: We are testing some email features....
    Email mail = new Email(EMAIL_FROM, "drew@august-harper.com", "test", "This is a test!", false);

    const string PASSWORD = "your password here!";
    var creds = new NetworkCredential(EMAIL_FROM, PASSWORD);

    Mailer.SendMail(mail, SMTP_SERVER, SMTP_PORT, false, creds);

    return 0;

    memberAccess = InitDatabase();

    if (HandleCommandLine(args, out int exitCode))
    {
      return exitCode;
    }


    var builder = WebApplication.CreateBuilder(args);


    builder.Services.AddSingleton<IMemberAccess>(memberAccess);
    builder.Services.AddSingleton<IEmailService>(new EmailService());


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
  private static bool HandleCommandLine(string[] args, out int exitCode)
  {
    exitCode = 0;

    try
    {
      // TODO: Proper commandline args?
      // Care about validation? --> Not at this time!
      // NOTE: A more secure scenario would require us to use a specific DB login/other creds
      // for this kind of thing.

      if (args.Length > 0 && args[0] == "delete-user")
      {
        string username = args[1];
        Console.WriteLine($"Deleting the user: {username}!");

        memberAccess.RemoveMember(username);
        Console.WriteLine($"DELETED!");

        return true;
      }

      if (args.Length > 0 && args[0] == "create-admin")
      {
        Console.WriteLine("Creating admin user for database!");

        string username = args[1];
        string email = args[2];
        string password = args[3];

        Member m = memberAccess.CreateMember(username, email, password);

        m.Permissions = "ADMIN";
        m.VerificationCode = "cli-created";
        m.VerifiedOn = DateTimeOffset.Now;

        memberAccess.UpdateMember(m);

        return true;
      }

      // ..... Other CLI stuff?

      return false;
    }
    catch (Exception ex)
    {
      // TODO: Log exception?
      Console.WriteLine(ex.Message);
      exitCode = 1;
    }

    // No special command line stuff was used.
    return false;
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

  // --------------------------------------------------------------------------------------------------------------------------
  private static SqliteMemberAccess InitDatabase()
  {
    // Add services to the container.
    // ENV:
    const string DATA_DIR = "data";
    const string DB_FILE = "member-man";
    var memberAccess = new SqliteMemberAccess(DATA_DIR, DB_FILE);
    if (!File.Exists(memberAccess.DBFilePath))
    {
      memberAccess.SetupDatabase();
    }

    return memberAccess;
  }
}