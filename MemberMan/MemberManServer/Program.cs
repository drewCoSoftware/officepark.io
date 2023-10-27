using System.Text.RegularExpressions;
using drewCo.Tools;
using officepark.io.Membership;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ENV:
const string DATA_DIR = "data";
const string DB_FILE = "member-man";
var memberAccess = new SqliteMemberAccess(DATA_DIR, DB_FILE);
if (!File.Exists(memberAccess.DBFilePath))
{
  memberAccess.SetupDatabase();
}

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

app.Run();


// --------------------------------------------------------------------------------------------------------------------------
// REFACTOR: This can be used in other projects and should be refactored into a lib??
static bool IsOriginAllowed(string host)
{
  // NOTE: These could come from some kind of updateable cache / settings file.
  var corsOriginAllowed = new[] { "localhost" };

  return corsOriginAllowed.Any(origin =>
      Regex.IsMatch(host, $@"^http(s)?://.*{origin}(:[0-9]+)?$", RegexOptions.IgnoreCase));
}
