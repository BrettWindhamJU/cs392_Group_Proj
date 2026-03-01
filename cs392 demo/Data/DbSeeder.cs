using cs392_demo.Constants;
using cs392_demo.models;
using Microsoft.AspNetCore.Identity;

namespace cs392_demo.Data
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            var userManager = service.GetRequiredService<UserManager<AppUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            if (!await roleManager.RoleExistsAsync(Roles.Owner.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Owner.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.Manager.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Manager.ToString()));

            var user = await userManager.FindByEmailAsync("sarah@test.com");

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = "Sarah2@test.com",
                    Email = "Sarah2@test.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Dogman123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Owner.ToString());
                }
                else
                {
                    foreach (var error in result.Errors)
                        Console.WriteLine(error.Description);
                }
            }
        }
    }
}



