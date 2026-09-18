using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using MyFn.Application;
using MyFn.Infrastructure;
using MyFn.Web.Components;

var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

var cloudPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(cloudPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{cloudPort}");
}
else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && string.IsNullOrWhiteSpace(builder.Configuration["Urls"]))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5081");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMyFnInfrastructure(builder.Configuration);
builder.Services.AddMyFnApplication();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
await app.Services.InitializeDatabaseAsync();

app.UseForwardedHeaders();

var httpsEnabled = UrlsIncludeHttps(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
                   || UrlsIncludeHttps(builder.Configuration["Urls"])
                   || string.Equals(Environment.GetEnvironmentVariable("MYFN_HTTPS"), "1", StringComparison.OrdinalIgnoreCase);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (httpsEnabled)
    {
        app.UseHsts();
    }
}

if (httpsEnabled)
{
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool UrlsIncludeHttps(string? urls) =>
    !string.IsNullOrWhiteSpace(urls)
    && urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
