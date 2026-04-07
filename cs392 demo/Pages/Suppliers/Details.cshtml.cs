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
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public DetailsModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        public Supplier Supplier { get; private set; } = new();

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

            Supplier? supplier;
            try
            {
                supplier = await _mongoService.GetBySupplierIdAsync(businessId, id);
            }
            catch (Exception ex) when (IsMongoConnectionIssue(ex))
            {
                TempData["SuppliersError"] = "Supplier database is temporarily unavailable. Please try again shortly.";
                return RedirectToPage("/Suppliers/Index");
            }

            if (supplier == null)
            {
                return NotFound();
            }

            Supplier = supplier;
            return Page();
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
    }
}
