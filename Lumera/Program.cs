using Microsoft.EntityFrameworkCore;
using Lumera.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Lumera.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// MySQL Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))
    ));

// Register all services
//builder.Services.AddScoped<IEmailService>(provider =>
//new EmailService(builder.Configuration));
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add Authentication Services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/LoginSignup";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
// CHANGED: Always use developer exception page for debugging
app.UseDeveloperExceptionPage(); // ADD THIS LINE

// COMMENT OUT the existing error handling:
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
//     app.UseHsts();
// }

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "organizer",
    pattern: "Organizer/{action=Dashboard}/{id?}",
    defaults: new { controller = "Organizer" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

app.MapControllerRoute(
    name: "deleteService",
    pattern: "organizer/services/delete/{id}",
    defaults: new { controller = "Organizer", action = "DeleteService" });

app.MapControllerRoute(
    name: "toggleService",
    pattern: "organizer/services/toggle/{id}",
    defaults: new { controller = "Organizer", action = "ToggleServiceStatus" });