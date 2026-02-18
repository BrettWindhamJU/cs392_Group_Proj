using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Inventory_Log
{
    public class DeleteModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DeleteModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Inventory_Activity_Log Inventory_Activity_Log { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(char? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory_activity_log = await _context.Inventory_Activity_Log.FirstOrDefaultAsync(m => m.Stock_ID_Log == id);

            if (inventory_activity_log == null)
            {
                return NotFound();
            }
            else
            {
                Inventory_Activity_Log = inventory_activity_log;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(char? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory_activity_log = await _context.Inventory_Activity_Log.FindAsync(id);
            if (inventory_activity_log != null)
            {
                Inventory_Activity_Log = inventory_activity_log;
                _context.Inventory_Activity_Log.Remove(Inventory_Activity_Log);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
