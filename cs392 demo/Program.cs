using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using cs392_demo.Data;
using Microsoft.AspNetCore.Components;
using cs392_demo;
using Microsoft.AspNetCore.Identity;
using cs392_demo.models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

//Replaced AddDbContextFactory for Identity purposes
builder.Services.AddDbContext<cs392_demoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("cs392_demoContext") 
        ?? throw new InvalidOperationException("Connection string 'cs392_demoContext' not found.")
    )
);

// Add Identity services
builder.Services.AddDefaultIdentity<AppUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Password policies enforced
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<cs392_demoContext>();

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

app.MapGet("/", () => Results.Redirect("/Identity/Account/Login?ReturnUrl=/Index"));

app.MapRazorPages();

app.Run();