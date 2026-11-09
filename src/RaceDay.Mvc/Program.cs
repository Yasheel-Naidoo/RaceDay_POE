using Microsoft.AspNetCore.Localization;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using System;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// The API origin is configurable so docker-compose (ApiSettings__BaseUrl) can point at the API container.
var apiBaseUrl = (builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/").TrimEnd('/');
RaceDayUrls.ApiBaseUrl = apiBaseUrl;

// Register HTTP client using IRaceDayApiClient
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiSessionCookieHandler>();
builder.Services.AddHttpClient<IRaceDayApiClient, RaceDayClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + "/");
})
.AddHttpMessageHandler<ApiSessionCookieHandler>()
// The API session is forwarded per MVC user by ApiSessionCookieHandler; the
// handler's own shared cookie jar would leak one user's session into another's call.
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

// Configure Session state for role-based authentication and navigation
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTML number inputs always post '.' as the decimal separator, so parse and format
// request values with the invariant culture rather than the machine's culture.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = new[] { CultureInfo.InvariantCulture },
    SupportedUICultures = new[] { CultureInfo.InvariantCulture }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();