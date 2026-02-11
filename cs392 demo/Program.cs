using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using cs392_demo.Data;
using Microsoft.AspNetCore.Components;
using cs392_demo;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<cs392_demoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("cs392_demoContext") ?? throw new InvalidOperationException("Connection string 'cs392_demoContext' not found.")));

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
