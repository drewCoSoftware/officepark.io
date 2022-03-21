using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddRazorPages();
//builder.Services.AddRouting();
builder.Services.AddControllers();

builder.Services.AddCors((ops) =>
{
  ops.AddPolicy("corsPolicy", builder =>
  {
    builder.SetIsOriginAllowed(IsOriginAllowed);
    builder.AllowCredentials();
//    builder.WithHeaders("content-type");
//    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
  });
});

static bool IsOriginAllowed(string host)
{
  // NOTE: These could come from some kind of updateable cache.
  var corsOriginAllowed = new[] { "localhost" };

  return corsOriginAllowed.Any(origin =>
      Regex.IsMatch(host, $@"^http(s)?://.*{origin}(:[0-9]+)?$", RegexOptions.IgnoreCase));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();


//app.MapRazorPages();
app.UseCors("corsPolicy");


app.Run();
