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
using System.Security.Claims;

namespace cs392_demo.Pages.Inventory_Log
{
    [Authorize(Roles = "Owner,Manager")]
    public class CreateModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public CreateModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateStockOptionsAsync();
            return Page();
        }

        [BindProperty]
        public Inventory_Activity_Log Inventory_Activity_Log { get; set; } = default!;

        public SelectList StockOptions { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            if (businessId == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not linked to a business.");
                await PopulateStockOptionsAsync();
                return Page();
            }

            Inventory_Activity_Log.BusinessId = businessId;
            Inventory_Activity_Log.Changed_By = User?.Identity?.Name ?? "System";
            Inventory_Activity_Log.Changed_At = DateTime.Now;

            ModelState.Remove("Inventory_Activity_Log.Changed_By");
            ModelState.Remove("Inventory_Activity_Log.Changed_At");

            if (await _context.Inventory_Activity_Log.AnyAsync(log => log.Log_ID == Inventory_Activity_Log.Log_ID))
            {
                ModelState.AddModelError("Inventory_Activity_Log.Log_ID", "That Log ID already exists. Please use a unique Log ID.");
            }

            if (!await _context.Stock.AnyAsync(s => s.Stock_ID == Inventory_Activity_Log.Stock_ID_Log && s.BusinessId == businessId))
            {
                ModelState.AddModelError("Inventory_Activity_Log.Stock_ID_Log", "Please select a valid Stock ID.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateStockOptionsAsync();
                return Page();
            }

            try
            {
                _context.Inventory_Activity_Log.Add(Inventory_Activity_Log);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Unable to save Inventory Log entry. {ex.GetBaseException().Message}");
                await PopulateStockOptionsAsync();
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private async Task PopulateStockOptionsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                StockOptions = new SelectList(Enumerable.Empty<string>());
                return;
            }

            var stockIds = await _context.Stock
                .Where(s => s.BusinessId == businessId)
                .OrderBy(s => s.Stock_ID)
                .Select(s => s.Stock_ID)
                .ToListAsync();

            StockOptions = new SelectList(stockIds);
        }
    }
}
