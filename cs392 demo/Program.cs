using cs392_demo;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// add mongodb services
builder.Services.AddSingleton<MongoDBService>();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<AIService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Replaced AddDbContextFactory for Identity purposes
builder.Services.AddDbContext<cs392_demoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("cs392_demoContext")
        ?? throw new InvalidOperationException("Connection string 'cs392_demoContext' not found."),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null))
);

// Add Identity services
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("cs392_demoContext")
        ?? throw new InvalidOperationException("Connection string'cs392_demoContext' not found."),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));
builder.Services.AddIdentity<AppUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
.AddEntityFrameworkStores<ApplicationContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MongoDBService>();



builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Required for Identity authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database seeding failed at startup. App will continue running.");
    }
}


app.Run();