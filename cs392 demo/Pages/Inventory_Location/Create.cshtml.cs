using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.cs360.Inventory_Location
{
    [Authorize(Roles = "Owner")]
    public class CreateModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public CreateModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }


        [BindProperty]
        public cs392_demo.models.Inventory_Location Inventory_Location { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Auto-assign the current user's business — do not trust form input for this
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Inventory_Location.BusinessId = currentUser?.BusinessId;
            Inventory_Location.Owner_User_ID = userId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Inventory_Location.location_id))
            {
                ModelState.AddModelError("Inventory_Location.location_id", "Location ID is required.");
            }

            // Prevent duplicate primary key exceptions from reaching the user.
            var alreadyExists = await _context.Inventory_Location
                .AnyAsync(l => l.BusinessId == Inventory_Location.BusinessId && l.location_id == Inventory_Location.location_id);

            if (alreadyExists)
            {
                ModelState.AddModelError("Inventory_Location.location_id", "That Location ID already exists. Please use a different ID or leave it blank to auto-generate.");
            }

            ModelState.Remove(nameof(Inventory_Location.BusinessId));

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _context.Inventory_Location.Add(Inventory_Location);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Inventory_Location.location_id", "Unable to save this location. The ID may already exist.");
                return Page();
            }

            return RedirectToPage("/Inventory_Location/Index");
        }
    }
}
