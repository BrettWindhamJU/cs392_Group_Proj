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
    public class EditModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public EditModel(cs392_demo.Data.cs392_demoContext context)
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
            Inventory_Location = inventory_location;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            // Keep tenant ownership on the server side.
            Inventory_Location.BusinessId = businessId;
            Inventory_Location.Owner_User_ID = userId ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.Inventory_Location
                .FirstOrDefaultAsync(l => l.BusinessId == businessId && l.location_id == Inventory_Location.location_id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.Location_name = Inventory_Location.Location_name;
            existing.Address_Location = Inventory_Location.Address_Location;
            existing.Owner_User_ID = userId ?? string.Empty;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Inventory_LocationExists(Inventory_Location.location_id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Inventory_Location/Index");
        }

        private bool Inventory_LocationExists(string id)
        {
            return _context.Inventory_Location.Any(e => e.location_id == id);
        }
    }
}
