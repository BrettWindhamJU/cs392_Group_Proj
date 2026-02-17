using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.cs360.Inventory_Location
{
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

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Inventory_Location.Add(Inventory_Location);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
