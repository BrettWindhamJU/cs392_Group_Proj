using cs392_demo;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// add mongodb services
builder.Services.AddSingleton<MongoDBServices>();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<AIService>();

//Replaced AddDbContextFactory for Identity purposes
builder.Services.AddDbContext<cs392_demoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("cs392_demoContext") 
        ?? throw new InvalidOperationException("Connection string 'cs392_demoContext' not found.")
    )
);

// Add Identity services
builder.Services.AddDbContext<ApplicationContext>(options =>options.UseSqlServer(builder.Configuration.GetConnectionString("cs392_demoContext") ?? throw new InvalidOperationException("Connection string'cs392_demoContext' not found.")));
builder.Services.AddIdentity<AppUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
.AddEntityFrameworkStores<ApplicationContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();



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

// Required for Identity authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}


app.Run();