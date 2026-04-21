using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace cs392_demo.Pages.Orders
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongo;

        public DetailsModel(cs392_demoContext context, MongoDBService mongo)
        {
            _context = context;
            _mongo = mongo;
        }

        public PurchaseOrder Order { get; set; } = null!;
        public bool IsOwner { get; set; }
        public string? LocationName { get; set; }
        public string? LocationAddress { get; set; }
        public string? SupplierAccountNumber { get; set; }
        public SupplierAddress? SupplierAddress { get; set; }
        public SupplierContact? SupplierContact { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId)) return Forbid();

            var order = await _context.PurchaseOrder
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);

            if (order == null) return NotFound();

            Order = order;
            IsOwner = User.IsInRole("Owner");

            if (!string.IsNullOrWhiteSpace(order.LocationId))
            {
                var loc = await _context.Inventory_Location
                    .FirstOrDefaultAsync(l => l.location_id == order.LocationId && l.BusinessId == businessId);
                LocationName = loc?.Location_name ?? order.LocationId;
                LocationAddress = loc?.Address_Location;
            }

            if (!string.IsNullOrWhiteSpace(order.SupplierMongoId))
            {
                var supplier = await _mongo.GetByMongoIdAsync(order.SupplierMongoId);
                SupplierAccountNumber = supplier?.AccountNumber;
                SupplierAddress = supplier?.Address;
                SupplierContact = supplier?.Contact;
            }

            return Page();
        }

        // POST: approve
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            if (!User.IsInRole("Owner")) return Forbid();
            return await TransitionAsync(id,
                new[] { PurchaseOrderStatus.Submitted },
                o =>
                {
                    o.Status = PurchaseOrderStatus.Approved;
                    o.ApprovedAt = DateTime.UtcNow;
                    o.ApprovedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                    o.ApprovedByUserName = User.Identity?.Name ?? string.Empty;
                });
        }

        // POST: mark as ordered
        public async Task<IActionResult> OnPostMarkOrderedAsync(int id)
        {
            if (!User.IsInRole("Owner") && !User.IsInRole("Manager")) return Forbid();
            return await TransitionAsync(id,
                new[] { PurchaseOrderStatus.Approved },
                o => o.Status = PurchaseOrderStatus.Ordered);
        }

        // POST: cancel
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            if (!User.IsInRole("Owner") && !User.IsInRole("Manager")) return Forbid();
            return await TransitionAsync(id,
                new[] {
                    PurchaseOrderStatus.Draft,
                    PurchaseOrderStatus.Submitted,
                    PurchaseOrderStatus.Approved,
                    PurchaseOrderStatus.Ordered
                },
                o => o.Status = PurchaseOrderStatus.Cancelled);
        }

        private async Task<IActionResult> TransitionAsync(
            int id,
            PurchaseOrderStatus[] allowedFrom,
            Action<PurchaseOrder> apply)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId)) return Forbid();

            var order = await _context.PurchaseOrder
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);

            if (order == null) return NotFound();

            if (!allowedFrom.Contains(order.Status))
            {
                TempData["Error"] = "Action not allowed in current status.";
                return RedirectToPage("./Details", new { id });
            }

            apply(order);
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id });
        }
    }
}
