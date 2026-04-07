using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;

namespace cs392_demo.Pages.Stock_Page
{
    [Authorize(Roles = "Owner,Manager")]
    public class EditModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;
        private readonly MongoDBServices _mongo;

        public EditModel(cs392_demoContext context, MongoDBServices mongo)
        {
            _context = context;
            _mongo = mongo;
        }

        [BindProperty]
        public Stock Stock { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            var stock = await _context.Stock.FirstOrDefaultAsync(m => m.Stock_ID == id && m.BusinessId == businessId);
            if (stock == null)
            {
                return NotFound();
            }
            Stock = stock;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var locationExistsInBusiness = await _context.Inventory_Location
                .AnyAsync(l => l.BusinessId == businessId && l.location_id == Stock.Location_Stock_ID);
            if (!locationExistsInBusiness)
            {
                ModelState.AddModelError("Stock.Location_Stock_ID", "That location does not exist in your business.");
                return Page();
            }

            var existingStock = await _context.Stock
                .FirstOrDefaultAsync(s => s.Stock_ID == Stock.Stock_ID && s.BusinessId == businessId);
            if (existingStock == null)
            {
                return NotFound();
            }

            var previousAmount = existingStock.Amount;
            var changedBy = User?.Identity?.Name ?? "System";
            var now = DateTime.Now;

            existingStock.Location_Stock_ID = Stock.Location_Stock_ID;
            existingStock.Item_Name = Stock.Item_Name;
            existingStock.SKU = Stock.SKU;
            existingStock.Amount = Stock.Amount;
            existingStock.Danger_Range = Stock.Danger_Range;
            existingStock.Last_Updated = now;
            existingStock.Last_Updated_by = changedBy;
            existingStock.BusinessId = businessId;

            try
            {

                if (previousAmount != existingStock.Amount)
                {
                    var log = new InventoryLog
                    {
                        Log_ID = Guid.NewGuid().ToString(), // Mongo-friendly ID
                        Stock_ID_Log = existingStock.Stock_ID,
                        BusinessId = businessId,
                        Quantity_Before = previousAmount,
                        Quantity_After = existingStock.Amount,
                        Changed_By = changedBy,
                        Changed_At = now
                    };

                    await _mongo.InventoryLog.InsertOneAsync(log);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StockExists(Stock.Stock_ID, businessId))
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

        private bool StockExists(string id, string businessId)
        {
            return _context.Stock.Any(e => e.Stock_ID == id && e.BusinessId == businessId);
        }

        private async Task<string> GenerateNextLogIdAsync()
        {
            var existingIds = await _context.Inventory_Activity_Log
                .Select(log => log.Log_ID)
                .ToListAsync();

            var maxNumber = 0;

            foreach (var logId in existingIds)
            {
                if (!logId.StartsWith("LOG-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(logId.Substring(4), out var parsedNumber) && parsedNumber > maxNumber)
                {
                    maxNumber = parsedNumber;
                }
            }

            var nextNumber = maxNumber + 1;
            var nextLogId = $"LOG-{nextNumber:D3}";

            while (await _context.Inventory_Activity_Log.AnyAsync(log => log.Log_ID == nextLogId))
            {
                nextNumber++;
                nextLogId = $"LOG-{nextNumber:D3}";
            }

            return nextLogId;
        }
    }
}
