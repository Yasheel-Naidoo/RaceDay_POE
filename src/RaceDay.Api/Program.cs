using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Static files are only served when wwwroot exists at startup, and uploads are
// written into it, so create it before the app is built.
var webRootPath = builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework with LocalDB
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register LocalFileService for handling local wwwroot uploads (No Azure / No Docker)
builder.Services.AddScoped<ILocalFileService, LocalFileService>();

// Session setup for API role tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register PasswordService for AuthController user registration
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Register the local blob service used by MediaController for banner uploads
builder.Services.AddScoped<IBlobService, LocalBlobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable serving static files from wwwroot (Crucial for local profile pictures & event banners)
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllers();

// Ensure Database is created and seeded, and close events whose date has passed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();

    var today = DateTime.UtcNow.Date;
    var staleEvents = db.Events
        .Where(e => e.EventDate < today && e.Status != "Closed")
        .ToList();

    if (staleEvents.Count > 0)
    {
        foreach (var staleEvent in staleEvents)
        {
            staleEvent.Status = "Closed";
        }
        db.SaveChanges();
    }
}

app.Run();