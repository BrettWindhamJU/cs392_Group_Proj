using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace cs392_demo.Pages.Users_Page
{
    [Authorize(Roles = "Owner, Manager")]
    public class IndexModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public IndexModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public IList<AppUser> Users { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                Users = new List<AppUser>();
                return;
            }

            Users = await _context.Users
                .Where(u => u.BusinessId == businessId)
                .ToListAsync();
        }
    }
}
