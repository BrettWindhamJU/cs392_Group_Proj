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

namespace cs392_demo.Pages.Inventory_Log
{
    [Authorize(Roles = "Owner,Manager")]
    public class EditModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public EditModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public SelectList StockOptions { get; set; } = default!;

        [BindProperty]
        public Inventory_Activity_Log Inventory_Activity_Log { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory_activity_log =  await _context.Inventory_Activity_Log.FirstOrDefaultAsync(m => m.Log_ID == id);
            if (inventory_activity_log == null)
            {
                return NotFound();
            }
            Inventory_Activity_Log = inventory_activity_log;
            await PopulateStockOptionsAsync();
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            Inventory_Activity_Log.Changed_By = User?.Identity?.Name ?? "System";
            Inventory_Activity_Log.Changed_At = DateTime.Now;

            if (!await _context.Stock.AnyAsync(s => s.Stock_ID == Inventory_Activity_Log.Stock_ID_Log))
            {
                ModelState.AddModelError("Inventory_Activity_Log.Stock_ID_Log", "Please select a valid Stock ID.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateStockOptionsAsync();
                return Page();
            }

            _context.Attach(Inventory_Activity_Log).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Inventory_Activity_LogExists(Inventory_Activity_Log.Log_ID))
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

        private bool Inventory_Activity_LogExists(string id)
        {
            return _context.Inventory_Activity_Log.Any(e => e.Log_ID == id);
        }

        private async Task PopulateStockOptionsAsync()
        {
            var stockIds = await _context.Stock
                .OrderBy(s => s.Stock_ID)
                .Select(s => s.Stock_ID)
                .ToListAsync();

            StockOptions = new SelectList(stockIds);
        }
    }
}
