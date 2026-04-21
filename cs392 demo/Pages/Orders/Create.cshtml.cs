using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InventoryLocation = cs392_demo.models.Inventory_Location;

namespace cs392_demo.Pages.Orders
{
    [Authorize(Roles = "Owner,Manager")]
    public class CreateModel : PageModel
    {
        public List<cs392_demo.models.Inventory_Location> Locations { get; set; } = new();
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public CreateModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        [BindProperty]
        public CreateOrderInput Input { get; set; } = new();

        /// When true, pre populate line items from low stock items
        [BindProperty(SupportsGet = true)]
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Stock> StockItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            Locations = string.IsNullOrWhiteSpace(businessId)
                ? new List<cs392_demo.models.Inventory_Location>()
                : await _context.Inventory_Location.Where(l => l.BusinessId == businessId).ToListAsync();
            if (!Input.LineItems.Any())
                Input.LineItems.Add(new LineItemInput());   // start with one blank row
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            Locations = string.IsNullOrWhiteSpace(businessId)
                ? new List<cs392_demo.models.Inventory_Location>()
                : await _context.Inventory_Location.Where(l => l.BusinessId == businessId).ToListAsync();

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
                await LoadDataAsync(businessId);
                return Page();
            }

            // Validate ExpectedDeliveryDate
            var today = DateTime.UtcNow.Date;
            var selectedSupplier2 = Suppliers.FirstOrDefault(s => s.Id == Input.SupplierMongoId);
            int leadTime = selectedSupplier2?.Terms?.LeadTimeDays ?? 0;
            var minDate = today.AddDays(leadTime);
            if (Input.ExpectedDeliveryDate < today)
            {
                ModelState.AddModelError("Input.ExpectedDeliveryDate", $"Delivery date can't be in the past. Please choose {today:MMM d, yyyy} or later.");
                await LoadDataAsync(businessId);
                return Page();
            }
            if (leadTime > 0 && Input.ExpectedDeliveryDate < minDate)
            {
                ModelState.AddModelError("Input.ExpectedDeliveryDate", $"{selectedSupplier2?.Name ?? "This supplier"} has a {leadTime}-day lead time. Earliest available date: {minDate:MMM d, yyyy}.");
                await LoadDataAsync(businessId);
                return Page();
            }

            // Validate that any selected stock items are in the chosen supplier's catalog
            var itemsWithStock = Input.LineItems.Where(li => !string.IsNullOrWhiteSpace(li.StockId)).ToList();
            if (itemsWithStock.Any())
            {
                Supplier? selectedSupplier = null;
                try
                {
                    var allSuppliers = await _mongoService.GetByBusinessAsync(businessId);
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
                        await LoadDataAsync(businessId);
                        return Page();
                    }
                }
            }

            // Build sequential PO number
            var nextNum = await _context.PurchaseOrder
                .Where(o => o.BusinessId == businessId)
                .CountAsync() + 1;

            var isOwner = User.IsInRole("Owner");
            var now     = DateTime.UtcNow;

            PurchaseOrderStatus status;
            if (Input.SaveAsDraft)
                status = PurchaseOrderStatus.Draft;
            else if (isOwner)
                status = PurchaseOrderStatus.Ordered;    // Owners self approve and go straight to Ordered
            else
                status = PurchaseOrderStatus.Submitted;  // Managers need Owner approval

            var order = new PurchaseOrder
            {
                BusinessId         = businessId,
                PONumber           = $"PO-{nextNum:D4}",
                SupplierMongoId    = Input.SupplierMongoId,
                SupplierName       = Input.SupplierName,
                Status             = status,
                CreatedAt          = now,
                UpdatedAt          = now,
                ExpectedDeliveryDate = Input.ExpectedDeliveryDate,
                Notes              = Input.Notes,
                CreatedByUserId    = userId ?? string.Empty,
                CreatedByUserName  = User.Identity?.Name ?? string.Empty,
                LocationId         = Input.LocationId,
                LineItems          = Input.LineItems.Select(li => new PurchaseOrderLineItem
                {
                    StockId         = li.StockId,
                    ItemName        = li.ItemName,
                    SKU             = li.SKU,
                    QuantityOrdered = li.QuantityOrdered,
                    UnitCost        = li.UnitCost
                }).ToList()
            };

            if (status == PurchaseOrderStatus.Submitted || status == PurchaseOrderStatus.Approved || status == PurchaseOrderStatus.Ordered)
                order.SubmittedAt = now;

            if (status == PurchaseOrderStatus.Approved || status == PurchaseOrderStatus.Ordered)
            {
                order.ApprovedAt         = now;
                order.ApprovedByUserId   = userId ?? string.Empty;
                order.ApprovedByUserName = User.Identity?.Name ?? string.Empty;
            }

            _context.PurchaseOrder.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = order.Id });
        }

        private async Task LoadDataAsync(string? businessId = null)
        {
            if (businessId == null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                businessId = currentUser?.BusinessId;
            }

            if (!string.IsNullOrWhiteSpace(businessId))
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


            }
        }
    }

    public class CreateOrderInput
    {
        [Required(ErrorMessage = "Please select a supplier.")]
        public string SupplierMongoId { get; set; } = string.Empty;

        [Required]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Expected Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please select a destination location.")]
        public string? LocationId { get; set; }

        ///True = save as Draft, False = submit immediately
        public bool SaveAsDraft { get; set; } = true;

        public List<LineItemInput> LineItems { get; set; } = new();
    }

    public class LineItemInput
    {
        public string? StockId { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        public string ItemName { get; set; } = string.Empty;

        public string? SKU { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int QuantityOrdered { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Cost cannot be negative.")]
        public decimal UnitCost { get; set; } = 0;
    }
}
