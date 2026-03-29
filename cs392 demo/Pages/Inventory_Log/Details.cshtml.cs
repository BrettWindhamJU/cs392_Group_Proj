using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;
using System.Security.Claims;

namespace cs392_demo.Pages.Inventory_Log
{
    [Authorize(Roles = "Owner,Manager")]
    public class DetailsModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DetailsModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public Inventory_Activity_Log Inventory_Activity_Log { get; set; } = default!;

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

            var inventory_activity_log = await _context.Inventory_Activity_Log
                .FirstOrDefaultAsync(m => m.Log_ID == id && m.BusinessId == businessId);
            if (inventory_activity_log == null)
            {
                return NotFound();
            }
            else
            {
                Inventory_Activity_Log = inventory_activity_log;
            }
            return Page();
        }
    }
}
