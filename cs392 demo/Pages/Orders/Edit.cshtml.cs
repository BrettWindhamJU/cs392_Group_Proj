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
    public class EditModel : PageModel
    {
        public List<Inventory_Location> Locations { get; set; } = new();
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public EditModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        [BindProperty]
        public EditOrderInput Input { get; set; } = new();

        public PurchaseOrder Order { get; set; } = null!;
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Stock> StockItems { get; set; } = new();
        public List<Stock> LowStockItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null) return NotFound();
            if (!order.IsEditable) return RedirectToPage("./Details", new { id });

            Order = order;
            await LoadDataAsync(order.BusinessId);
            Locations = string.IsNullOrWhiteSpace(order.BusinessId)
                ? new List<Inventory_Location>()
                : await _context.Inventory_Location.Where(l => l.BusinessId == order.BusinessId).ToListAsync();

            Input = new EditOrderInput
            {
                SupplierMongoId      = order.SupplierMongoId,
                SupplierName         = order.SupplierName,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                Notes                = order.Notes,
                LocationId           = order.LocationId,
                LineItems            = order.LineItems.Select(li => new EditLineItemInput
                {
                    Id              = li.Id,
                    StockId         = li.StockId,
                    ItemName        = li.ItemName,
                    SKU             = li.SKU,
                    QuantityOrdered = li.QuantityOrdered,
                    UnitCost        = li.UnitCost
                }).ToList()
            };

            if (!Input.LineItems.Any())
                Input.LineItems.Add(new EditLineItemInput());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null) return NotFound();
            if (!order.IsEditable) return RedirectToPage("./Details", new { id });

            Locations = string.IsNullOrWhiteSpace(order.BusinessId)
                ? new List<Inventory_Location>()
                : await _context.Inventory_Location.Where(l => l.BusinessId == order.BusinessId).ToListAsync();

            Input.LineItems = Input.LineItems
                .Where(li => !string.IsNullOrWhiteSpace(li.ItemName))
                .ToList();

            if (!Input.LineItems.Any())
                ModelState.AddModelError(string.Empty, "Add at least one item to the order before submitting.");

            // Prevent duplicate stock items
            var duplicateStockIds = Input.LineItems
                .Where(li => !string.IsNullOrWhiteSpace(li.StockId))
                .GroupBy(li => li.StockId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().ItemName)
                .ToList();
            if (duplicateStockIds.Any())
                ModelState.AddModelError(string.Empty, $"Duplicate items detected: {string.Join(", ", duplicateStockIds)}. Each stock item can appear only once — adjust the quantity instead.");

            if (!ModelState.IsValid)
            {
                Order = order;
                await LoadDataAsync(order.BusinessId);
                return Page();
            }

            // Validate stock items are in the supplier's catalog
            var itemsWithStock = Input.LineItems.Where(li => !string.IsNullOrWhiteSpace(li.StockId)).ToList();
            if (itemsWithStock.Any())
            {
                Supplier? selectedSupplier = null;
                try
                {
                    var allSuppliers = await _mongoService.GetByBusinessAsync(order.BusinessId);
                    selectedSupplier = allSuppliers.FirstOrDefault(s => s.Id == Input.SupplierMongoId);
                }
                catch { }

                if (selectedSupplier != null && selectedSupplier.Catalog.Any())
                {
                    var catalogStockIds = selectedSupplier.Catalog.Select(c => c.StockId).ToHashSet();
                    var invalidItems = itemsWithStock.Where(li => !catalogStockIds.Contains(li.StockId!)).ToList();
                    if (invalidItems.Any())
                    {
                        var names = string.Join(", ", invalidItems.Select(li => li.ItemName));
                        ModelState.AddModelError(string.Empty, $"The following items are not in {selectedSupplier.Name}'s catalog: {names}. Remove them or clear the stock selection.");
                        Order = order;
                        await LoadDataAsync(order.BusinessId);
                        return Page();
                    }
                }
            }

            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isOwner  = User.IsInRole("Owner");
            var now      = DateTime.UtcNow;

            // Validate ExpectedDeliveryDate
            var today = DateTime.UtcNow.Date;
            var selectedSupplier2 = Suppliers.FirstOrDefault(s => s.Id == Input.SupplierMongoId);
            int leadTime = selectedSupplier2?.Terms?.LeadTimeDays ?? 0;
            var minDate = today.AddDays(leadTime);
            if (Input.ExpectedDeliveryDate < today)
            {
                ModelState.AddModelError("Input.ExpectedDeliveryDate", $"Delivery date can't be in the past. Please choose {today:MMM d, yyyy} or later.");
                Order = order;
                await LoadDataAsync(order.BusinessId);
                return Page();
            }
            if (leadTime > 0 && Input.ExpectedDeliveryDate < minDate)
            {
                ModelState.AddModelError("Input.ExpectedDeliveryDate", $"{selectedSupplier2?.Name ?? "This supplier"} has a {leadTime}-day lead time. Earliest available date: {minDate:MMM d, yyyy}.");
                Order = order;
                await LoadDataAsync(order.BusinessId);
                return Page();
            }

            order.SupplierMongoId      = Input.SupplierMongoId;
            order.SupplierName         = Input.SupplierName;
            order.ExpectedDeliveryDate = Input.ExpectedDeliveryDate;
            order.Notes                = Input.Notes;
            order.LocationId           = Input.LocationId;
            order.UpdatedAt            = now;

            if (!Input.SaveAsDraft)
            {
                order.Status      = isOwner ? PurchaseOrderStatus.Ordered : PurchaseOrderStatus.Submitted;
                order.SubmittedAt = now;
                if (isOwner)
                {
                    order.ApprovedAt         = now;
                    order.ApprovedByUserId   = userId ?? string.Empty;
                    order.ApprovedByUserName = User.Identity?.Name ?? string.Empty;
                }
            }

            // Replace line items
            _context.PurchaseOrderLineItem.RemoveRange(order.LineItems);
            order.LineItems = Input.LineItems.Select(li => new PurchaseOrderLineItem
            {
                PurchaseOrderId = order.Id,
                StockId         = li.StockId,
                ItemName        = li.ItemName,
                SKU             = li.SKU,
                QuantityOrdered = li.QuantityOrdered,
                UnitCost        = li.UnitCost
            }).ToList();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id });
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

        private async Task LoadDataAsync(string businessId)
        {
            try
            {
                Suppliers = (await _mongoService.GetByBusinessAsync(businessId))
                    .Where(s => s.Status != "Inactive")
                    .OrderBy(s => s.Name)
                    .ToList();
            }
            catch { Suppliers = new(); }

            StockItems = await _context.Stock
                .Where(s => s.BusinessId == businessId)
                .OrderBy(s => s.Item_Name)
                .ToListAsync();

            LowStockItems = StockItems.Where(s => s.Amount <= s.Danger_Range).ToList();
        }
    }

    public class EditOrderInput
    {
        [Required(ErrorMessage = "Please select a supplier.")]
        public string SupplierMongoId { get; set; } = string.Empty;

        [Required]
        public string SupplierName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }
        public bool SaveAsDraft { get; set; } = true;

        [Required(ErrorMessage = "Please select a destination location.")]
        public string? LocationId { get; set; }

        public List<EditLineItemInput> LineItems { get; set; } = new();
    }

    public class EditLineItemInput
    {
        public int Id { get; set; }
        public string? StockId { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        public string ItemName { get; set; } = string.Empty;

        public string? SKU { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int QuantityOrdered { get; set; } = 1;

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; } = 0;
    }
}
