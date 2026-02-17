using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Stock_Page
{
    public class DeleteModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DeleteModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Stock Stock { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(char? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.Stock.FirstOrDefaultAsync(m => m.Stock_ID == id);

            if (stock == null)
            {
                return NotFound();
            }
            else
            {
                Stock = stock;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(char? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.Stock.FindAsync(id);
            if (stock != null)
            {
                Stock = stock;
                _context.Stock.Remove(Stock);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
