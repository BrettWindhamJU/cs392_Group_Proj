using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace cs392_demo.Pages.Orders
{
    [Authorize(Roles = "Owner,Manager")]
    public class ReceiveModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongo;

        public ReceiveModel(cs392_demoContext context, MongoDBService mongo)
        {
            _context = context;
            _mongo = mongo;
        }

        public PurchaseOrder Order { get; set; } = null!;

        [BindProperty]
        public List<ReceiveLineInput> Lines { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null) return NotFound();
            if (!order.CanReceive) return RedirectToPage("./Details", new { id });

            Order = order;
            Lines = order.LineItems
                .Select(li => new ReceiveLineInput
                {
                    LineItemId       = li.Id,
                    ItemName         = li.ItemName,
                    QuantityOrdered  = li.QuantityOrdered,
                    QuantityReceived = li.QuantityReceived,
                    QuantityToReceive = 0
                }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId)) return Forbid();

            var order = await _context.PurchaseOrder
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);

            if (order == null) return NotFound();
            if (!order.CanReceive) return RedirectToPage("./Details", new { id });

            var now      = DateTime.UtcNow;
            var userName = User.Identity?.Name ?? userId ?? string.Empty;
            bool anyReceived = false;

            // Pre compute starting SQL log ID so the loop doesn't re query before SaveChanges
            int nextSqlLogNum = await GetNextSqlLogIdAsync();

            foreach (var input in Lines)
            {
                if (input.QuantityToReceive <= 0) continue;

                var lineItem = order.LineItems.FirstOrDefault(li => li.Id == input.LineItemId);
                if (lineItem == null) continue;

                var maxReceivable = lineItem.QuantityOrdered - lineItem.QuantityReceived;
                var actualQty = Math.Min(input.QuantityToReceive, maxReceivable);
                if (actualQty <= 0) continue;

                lineItem.QuantityReceived += actualQty;

                // Update stock if linked to a stock item
                if (!string.IsNullOrWhiteSpace(lineItem.StockId))
                {
                    // Match by StockId AND location from the order header
                    Stock? stock = null;
                    if (!string.IsNullOrWhiteSpace(order.LocationId))
                    {
                        stock = await _context.Stock
                            .FirstOrDefaultAsync(s => s.Stock_ID == lineItem.StockId
                                                   && s.Location_Stock_ID == order.LocationId
                                                   && s.BusinessId == businessId);

                        // If no stock record exists at this location, create one from an existing record
                        if (stock == null)
                        {
                            var template = await _context.Stock
                                .FirstOrDefaultAsync(s => s.Stock_ID == lineItem.StockId && s.BusinessId == businessId);

                            if (template != null)
                            {
                                var newStockId = await GenerateNextStockIdAsync(businessId);
                                stock = new Stock
                                {
                                    Stock_ID         = newStockId,
                                    Location_Stock_ID = order.LocationId,
                                    BusinessId       = businessId,
                                    Item_Name        = template.Item_Name,
                                    SKU              = template.SKU,
                                    Amount           = 0,
                                    Danger_Range     = template.Danger_Range,
                                    Last_Updated     = now,
                                    Last_Updated_by  = $"{order.PONumber} ({userName})"
                                };
                                _context.Stock.Add(stock);
                                await _context.SaveChangesAsync(); // get the new Stock_Key

                                // Update the line item to point to the new stock record
                                // so future partial receives find the correct one
                                lineItem.StockId = newStockId;
                            }
                        }
                    }
                    else
                    {
                        // No location on line item — fall back to first match
                        stock = await _context.Stock
                            .FirstOrDefaultAsync(s => s.Stock_ID == lineItem.StockId && s.BusinessId == businessId);
                    }

                    if (stock != null)
                    {
                        var previousAmount = stock.Amount;
                        stock.Amount += actualQty;
                        stock.Last_Updated = now;
                        stock.Last_Updated_by = $"{order.PONumber} ({userName})";

                        var restockLabel = $"{order.PONumber} ({userName})";

                        // MongoDB audit log (matches Stock_Page/Edit pattern)
                        var mongoLog = new InventoryLog
                        {
                            Log_ID       = Guid.NewGuid().ToString(),
                            Stock_ID_Log = stock.Stock_ID,
                            BusinessId   = businessId,
                            Quantity_Before = previousAmount,
                            Quantity_After  = stock.Amount,
                            Changed_By   = restockLabel,
                            Changed_At   = now
                        };
                        await _mongo.InventoryLog.InsertOneAsync(mongoLog);

                        // SQL activity log
                        _context.Inventory_Activity_Log.Add(new Inventory_Activity_Log
                        {
                            Log_ID          = $"LOG-{nextSqlLogNum++:D3}",
                            Stock_ID_Log    = stock.Stock_ID,
                            BusinessId      = businessId,
                            Quantity_Before = previousAmount,
                            Quantity_After  = stock.Amount,
                            Changed_By      = restockLabel,
                            Changed_At      = now
                        });
                    }
                }

                anyReceived = true;
            }

            if (!anyReceived)
            {
                Order = order;
                Lines = order.LineItems.Select(li => new ReceiveLineInput
                {
                    LineItemId        = li.Id,
                    ItemName          = li.ItemName,
                    QuantityOrdered   = li.QuantityOrdered,
                    QuantityReceived  = li.QuantityReceived,
                    QuantityToReceive = 0
                }).ToList();
                ModelState.AddModelError(string.Empty, "Enter a quantity for at least one item.");
                return Page();
            }

            // Recalculate PO status
            bool allReceived = order.LineItems.All(li => li.QuantityReceived >= li.QuantityOrdered);
            order.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
            order.UpdatedAt = now;

            if (allReceived)
            {
                order.ReceivedAt          = now;
                order.ReceivedByUserId    = userId ?? string.Empty;
                order.ReceivedByUserName  = userName;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id });
        }

        private async Task<IActionResult?> LoadErrorPage(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null) return NotFound();
            Order = order;
            return null;
        }

        private async Task<PurchaseOrder?> LoadOrderAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            if (string.IsNullOrWhiteSpace(businessId)) return null;

            return await _context.PurchaseOrder
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);
        }

        private async Task<int> GetNextSqlLogIdAsync()
        {
            var existingIds = await _context.Inventory_Activity_Log
                .Select(l => l.Log_ID)
                .ToListAsync();

            var maxNumber = 0;
            foreach (var id in existingIds)
            {
                if (id.StartsWith("LOG-", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(id.Substring(4), out var n) && n > maxNumber)
                    maxNumber = n;
            }
            return maxNumber + 1;
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
                var match = System.Text.RegularExpressions.Regex.Match(id ?? string.Empty, @"^S-(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > maxNumber)
                    maxNumber = parsed;
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
    }

    public class ReceiveLineInput
    {
        public int LineItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }

        [Range(0, int.MaxValue)]
        public int QuantityToReceive { get; set; }
    }
}
