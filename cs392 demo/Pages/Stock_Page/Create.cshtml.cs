using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Stock_Page
{
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
        public string Location_Id { get; set; } = string.Empty;
        public void OnGet(){
            Stock = new Stock
            {
                Location_Stock_ID = Location_Id,
                Last_Updated = DateTime.Now,
                Last_Updated_by = DateTime.Now
            };
        }

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Stock.Last_Updated == null)
            {
                Stock.Last_Updated = DateTime.Now;
            }

            if (Stock.Last_Updated_by == null)
            {
                Stock.Last_Updated_by = DateTime.Now;
            }

            _context.Stock.Add(Stock);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
