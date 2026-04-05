using cs392_demo.Constants;
using cs392_demo.models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace cs392_demo.Data
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            var userManager = service.GetRequiredService<UserManager<AppUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();
            var context = service.GetRequiredService<cs392_demoContext>();

            // Create roles if they don't exist
            if (!await roleManager.RoleExistsAsync(Roles.Owner.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Owner.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.Manager.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Manager.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.User.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.User.ToString()));

            // Ensure the seed business exists
            const string seedBusinessId = "BIZ-SEED-001";
            const string seedInviteCode = "STOCKWISE";

            var seedBusiness = await context.Business.FirstOrDefaultAsync(b => b.Business_ID == seedBusinessId);
            if (seedBusiness == null)
            {
                seedBusiness = new Business
                {
                    Business_ID = seedBusinessId,
                    Business_Name = "Demo Business Co.",
                    Description = "Seed business for demonstration purposes.",
                    Invite_Code = seedInviteCode
                };
                context.Business.Add(seedBusiness);
                await context.SaveChangesAsync();
            }

            // Seed the default owner account (fix: check by the actual email used to create)
            var user = await userManager.FindByEmailAsync("Sarah2@test.com");

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = "Sarah2@test.com",
                    Email = "Sarah2@test.com",
                    EmailConfirmed = true,
                    BusinessId = seedBusinessId
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
            else if (user.BusinessId == null)
            {
                // Link existing owner to the seed business if they aren't linked yet
                user.BusinessId = seedBusinessId;
                await userManager.UpdateAsync(user);
            }

            // Link any existing locations that have no BusinessId to the seed business
            var unlinkedLocations = await context.Inventory_Location
                .Where(l => l.BusinessId == null)
                .ToListAsync();

            if (unlinkedLocations.Count > 0)
            {
                foreach (var loc in unlinkedLocations)
                    loc.BusinessId = seedBusinessId;

                await context.SaveChangesAsync();
            }

            // Link any existing stock rows that have no BusinessId.
            var unlinkedStock = await context.Stock
                .Where(s => s.BusinessId == null)
                .ToListAsync();

            if (unlinkedStock.Count > 0)
            {
                foreach (var stock in unlinkedStock)
                {
                    var matchingLocation = await context.Inventory_Location
                        .FirstOrDefaultAsync(l => l.location_id == stock.Location_Stock_ID && l.BusinessId != null);

                    stock.BusinessId = matchingLocation?.BusinessId ?? seedBusinessId;
                }

                await context.SaveChangesAsync();
            }

            // Backfill log BusinessId from stock where possible.
            var logsWithoutBusiness = await context.Inventory_Activity_Log
                .Where(l => l.BusinessId == null)
                .ToListAsync();

            if (logsWithoutBusiness.Count > 0)
            {
                foreach (var log in logsWithoutBusiness)
                {
                    var stock = await context.Stock
                        .FirstOrDefaultAsync(s => s.Stock_ID == log.Stock_ID_Log && s.BusinessId != null);

                    log.BusinessId = stock?.BusinessId ?? seedBusinessId;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}

