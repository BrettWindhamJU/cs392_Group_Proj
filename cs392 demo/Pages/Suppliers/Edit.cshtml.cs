using System.Security.Claims;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace cs392_demo.Pages.Suppliers
{
    [Authorize(Roles = "Owner,Manager")]
    public class EditModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public EditModel(cs392_demoContext context, MongoDBService mongoService)
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

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                return NotFound();
            }

            var supplier = await _mongoService.GetBySupplierIdAsync(businessId, id);
            if (supplier == null)
            {
                return NotFound();
            }

            Supplier = supplier;
            CategoriesInput = string.Join(", ", Supplier.Categories);
            DeliveryDaysInput = string.Join(", ", Supplier.Terms.DeliveryDays);
            CatalogItems = Supplier.Catalog.ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                return NotFound();
            }

            var existing = await _mongoService.GetBySupplierIdAsync(businessId, id);
            if (existing == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(Supplier.Name))
            {
                ModelState.AddModelError("Supplier.Name", "Supplier name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Supplier.Id = existing.Id;
            Supplier.BusinessId = businessId;
            Supplier.SupplierId = existing.SupplierId;
            Supplier.CreatedAtUtc = existing.CreatedAtUtc;
            Supplier.UpdatedAtUtc = DateTime.UtcNow;
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

            await _mongoService.UpdateAsync(businessId, existing.SupplierId, Supplier);
            return RedirectToPage("/Suppliers/Index");
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
