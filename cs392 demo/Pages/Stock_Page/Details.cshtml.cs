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
    public class DetailsModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DetailsModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

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
    }
}
