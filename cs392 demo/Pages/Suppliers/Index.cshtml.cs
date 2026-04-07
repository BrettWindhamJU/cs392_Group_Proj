using System.Security.Claims;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace cs392_demo.Pages.Suppliers
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public IndexModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        public List<Supplier> Suppliers { get; private set; } = new();
        public string CurrentBusinessId { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            CurrentBusinessId = businessId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                Suppliers = new List<Supplier>();
                return;
            }

            Suppliers = await _mongoService.GetByBusinessAsync(businessId);
            Suppliers = Suppliers.OrderBy(s => s.Name).ToList();
        }
    }
}
