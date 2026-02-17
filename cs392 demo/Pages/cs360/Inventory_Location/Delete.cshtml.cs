using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.cs360.Inventory_Location
{
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

            var inventory_location = await _context.Inventory_Location.FirstOrDefaultAsync(m => m.location_id == id);

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

            var inventory_location = await _context.Inventory_Location.FindAsync(id);
            if (inventory_location != null)
            {
                Inventory_Location = inventory_location;
                _context.Inventory_Location.Remove(Inventory_Location);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
