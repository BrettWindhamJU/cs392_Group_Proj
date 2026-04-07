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

            var supplier = await _mongoService.GetBySupplierIdAsync(businessId, id);
            if (supplier == null)
            {
                return NotFound();
            }

            Supplier = supplier;
            return Page();
        }
    }
}
