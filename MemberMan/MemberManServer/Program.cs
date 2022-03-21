using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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
