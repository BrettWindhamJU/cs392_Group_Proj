using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Claims;
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not linked to a business.");
                return Page();
            }

            Stock.BusinessId = businessId;

            var locationExistsInBusiness = await _context.Inventory_Location
                .AnyAsync(l => l.BusinessId == businessId && l.location_id == Stock.Location_Stock_ID);

            if (!locationExistsInBusiness)
            {
                ModelState.AddModelError("Stock.Location_Stock_ID", "That location does not exist in your business.");
            }

            if (string.IsNullOrWhiteSpace(Stock.Stock_ID))
            {
                Stock.Stock_ID = await GenerateNextStockIdAsync(businessId);
                ModelState.Remove("Stock.Stock_ID");
            }

            var duplicateStockId = await _context.Stock.AnyAsync(s => s.Stock_ID == Stock.Stock_ID && s.BusinessId == businessId);
            if (duplicateStockId)
            {
                ModelState.AddModelError("Stock.Stock_ID", "That Stock ID already exists in your business. Please use a unique Stock ID or leave it blank to auto-generate.");
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

                var changedBy = User?.Identity?.Name ?? "System";
                var now = DateTime.Now;
                var nextLogId = await GenerateNextLogIdAsync();

                _context.Inventory_Activity_Log.Add(new Inventory_Activity_Log
                {
                    Log_ID = nextLogId,
                    Stock_ID_Log = Stock.Stock_ID,
                    BusinessId = businessId,
                    Quantity_Before = 0,
                    Quantity_After = Stock.Amount,
                    Changed_By = changedBy,
                    Changed_At = now
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Stock.Stock_ID", "That Stock ID already exists. Please use a unique Stock ID.");
                return Page();
            }

            return RedirectToPage("/Stock_Page/Index");
        }

        private async Task<string> GenerateNextStockIdAsync(string businessId)
        {
            var existingIds = await _context.Stock
                .Where(s => s.BusinessId == businessId)
                .Select(s => s.Stock_ID)
                .ToListAsync();

            var maxNumber = 0;
            foreach (var id in existingIds)
            {
                var match = Regex.Match(id ?? string.Empty, @"^S-(\d+)$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    continue;
                }

                if (int.TryParse(match.Groups[1].Value, out var parsed) && parsed > maxNumber)
                {
                    maxNumber = parsed;
                }
            }

            var nextNumber = maxNumber + 1;
            var nextId = $"S-{nextNumber:D3}";

            while (await _context.Stock.AnyAsync(s => s.Stock_ID == nextId && s.BusinessId == businessId))
            {
                nextNumber++;
                nextId = $"S-{nextNumber:D3}";
            }

            return nextId;
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
