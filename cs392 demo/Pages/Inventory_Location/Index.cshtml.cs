using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.cs360.Inventory_Location
{
    [Authorize(Roles = "Owner,Manager")]
    public class IndexModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public IndexModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public IList<cs392_demo.models.Inventory_Location> Inventory_Location { get; set; } = default!;
        public Dictionary<string, string> OwnerDisplayByUserId { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                Inventory_Location = new List<cs392_demo.models.Inventory_Location>();
                return;
            }

            Inventory_Location = await _context.Inventory_Location
                .Where(l => l.BusinessId == businessId)
                .ToListAsync();

            var ownerIds = Inventory_Location
                .Select(l => l.Owner_User_ID)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (ownerIds.Count > 0)
            {
                OwnerDisplayByUserId = await _context.Users
                    .Where(u => ownerIds.Contains(u.Id))
                    .ToDictionaryAsync(
                        u => u.Id,
                        u => string.IsNullOrWhiteSpace(u.Email) ? (u.UserName ?? "Unknown") : u.Email);
            }
        }

        public string GetOwnerDisplay(string ownerUserId)
        {
            if (string.IsNullOrWhiteSpace(ownerUserId))
            {
                return "Unassigned";
            }

            return OwnerDisplayByUserId.TryGetValue(ownerUserId, out var display)
                ? display
                : "Unknown User";
        }
    }
}
