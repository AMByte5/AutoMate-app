using AutoMate_app.Data;
using AutoMate_app.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMate_app.Filters;
using AutoMate_app.Models.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<GoogleMapsOptions>()
    .Bind(builder.Configuration.GetSection(GoogleMapsOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "GoogleMaps:ApiKey is missing")
    .ValidateOnStart();

builder.Services.AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Gemini:ApiKey is missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model), "Gemini:Model is missing")
    .ValidateOnStart();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
builder.Services.AddScoped<RequireMechanicProfileFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<RequireMechanicProfileFilter>();
});
builder.Services.AddHttpClient<GeminiAdvisorService>();
builder.Services.AddHttpClient<ChatService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
