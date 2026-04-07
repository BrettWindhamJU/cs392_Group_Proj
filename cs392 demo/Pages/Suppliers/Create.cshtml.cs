using System.Security.Claims;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace cs392_demo.Pages.Suppliers
{
    [Authorize(Roles = "Owner,Manager")]
    public class CreateModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public CreateModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        [BindProperty]
        public Supplier Supplier { get; set; } = new();

        [BindProperty]
        public string CategoriesInput { get; set; } = string.Empty;

        [BindProperty]
        public string DeliveryDaysInput { get; set; } = string.Empty;

        [BindProperty]
        public List<SupplierCatalogItem> CatalogItems { get; set; } = new();

        public void OnGet()
        {
            // Intentionally left blank so dropdown placeholders are shown.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(Supplier.SupplierId))
            {
                ModelState.AddModelError("Supplier.SupplierId", "Supplier ID is required.");
            }

            if (string.IsNullOrWhiteSpace(Supplier.Name))
            {
                ModelState.AddModelError("Supplier.Name", "Supplier name is required.");
            }

            Supplier? existing = null;
            try
            {
                existing = await _mongoService.GetBySupplierIdAsync(businessId, Supplier.SupplierId.Trim());
            }
            catch (Exception ex) when (IsMongoConnectionIssue(ex))
            {
                ModelState.AddModelError(string.Empty, "Supplier database is temporarily unavailable. Please try again shortly.");
                return Page();
            }

            if (existing != null)
            {
                ModelState.AddModelError("Supplier.SupplierId", "That supplier ID already exists for this business.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Supplier.BusinessId = businessId;
            Supplier.SupplierId = Supplier.SupplierId.Trim();
            Supplier.Name = Supplier.Name.Trim();
            Supplier.Categories = ParseCsv(CategoriesInput);
            Supplier.Terms.DeliveryDays = ParseCsv(DeliveryDaysInput);
            Supplier.Catalog = (CatalogItems ?? new List<SupplierCatalogItem>())
                .Where(i =>
                    !string.IsNullOrWhiteSpace(i.StockId) ||
                    !string.IsNullOrWhiteSpace(i.SupplierSku) ||
                    !string.IsNullOrWhiteSpace(i.Unit) ||
                    !string.IsNullOrWhiteSpace(i.PackSize) ||
                    i.LastUnitPrice.HasValue)
                .Select(i => new SupplierCatalogItem
                {
                    StockId = (i.StockId ?? string.Empty).Trim(),
                    SupplierSku = (i.SupplierSku ?? string.Empty).Trim(),
                    Unit = (i.Unit ?? string.Empty).Trim(),
                    PackSize = (i.PackSize ?? string.Empty).Trim(),
                    LastUnitPrice = i.LastUnitPrice
                })
                .ToList();

            Supplier.CreatedAtUtc = DateTime.UtcNow;
            Supplier.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await _mongoService.CreateAsync(Supplier);
            }
            catch (Exception ex) when (IsMongoConnectionIssue(ex))
            {
                ModelState.AddModelError(string.Empty, "Supplier database is temporarily unavailable. Please try again shortly.");
                return Page();
            }

            return RedirectToPage("/Suppliers/Index");
        }

        private static bool IsMongoConnectionIssue(Exception ex)
        {
            var message = ex.ToString();
            return ex is MongoConnectionException
                || ex is TimeoutException
                || message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || message.Contains("DnsClient", StringComparison.OrdinalIgnoreCase)
                || message.Contains("server selection", StringComparison.OrdinalIgnoreCase)
                || message.Contains("mongod", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ParseCsv(string input)
        {
            return input
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
