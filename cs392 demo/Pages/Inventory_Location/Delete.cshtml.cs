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
    [Authorize(Roles = "Owner")]
    public class DeleteModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DeleteModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public cs392_demo.models.Inventory_Location Inventory_Location { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            var inventory_location = await _context.Inventory_Location
                .FirstOrDefaultAsync(m => m.location_id == id && m.BusinessId == businessId);

            if (inventory_location == null)
            {
                return NotFound();
            }
            else
            {
                Inventory_Location = inventory_location;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            var inventory_location = await _context.Inventory_Location
                .FirstOrDefaultAsync(l => l.location_id == id && l.BusinessId == businessId);
            if (inventory_location != null)
            {
                Inventory_Location = inventory_location;
                _context.Inventory_Location.Remove(Inventory_Location);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Inventory_Location/Index");
        }
    }
}
