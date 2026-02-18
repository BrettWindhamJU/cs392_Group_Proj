using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Inventory_Log
{
    public class EditModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public EditModel(cs392_demo.Data.cs392_demoContext context)
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

            var inventory_activity_log =  await _context.Inventory_Activity_Log.FirstOrDefaultAsync(m => m.Stock_ID_Log == id);
            if (inventory_activity_log == null)
            {
                return NotFound();
            }
            Inventory_Activity_Log = inventory_activity_log;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Inventory_Activity_Log).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Inventory_Activity_LogExists(Inventory_Activity_Log.Stock_ID_Log))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool Inventory_Activity_LogExists(char id)
        {
            return _context.Inventory_Activity_Log.Any(e => e.Stock_ID_Log == id);
        }
    }
}
