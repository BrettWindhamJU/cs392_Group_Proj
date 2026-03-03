using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Stock_Page
{
    [Authorize(Roles = "Owner,Manager")]
    public class CreateModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public CreateModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Stock Stock { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Location_Id { get; set; }
        public void OnGet(){
            var now = DateTime.Now;
            Stock = new Stock();
            Stock.Location_Stock_ID = Location_Id ?? string.Empty;
            Stock.Last_Updated = now;
            Stock.Last_Updated_by = User?.Identity?.Name ?? "System";
        }

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Stock.Location_Stock_ID) && !string.IsNullOrWhiteSpace(Location_Id))
            {
                Stock.Location_Stock_ID = Location_Id;
            }

            Stock.Last_Updated = DateTime.Now;
            Stock.Last_Updated_by = User?.Identity?.Name ?? "System";

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _context.Stock.Add(Stock);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Stock.Stock_ID", "That Stock ID already exists. Please use a unique Stock ID.");
                return Page();
            }

            return RedirectToPage("/Stock_Page/Index");
        }
    }
}
