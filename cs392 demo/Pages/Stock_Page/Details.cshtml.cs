using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Stock_Page
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DetailsModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DetailsModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public Stock Stock { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                return NotFound();
            }

            var stock = await _context.Stock
                .FirstOrDefaultAsync(m => m.Stock_ID == id && m.BusinessId == businessId);
            if (stock == null)
            {
                return NotFound();
            }
            else
            {
                Stock = stock;
            }
            return Page();
        }
    }
}
