using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;

namespace cs392_demo.Pages.Users_Page
{
    [Authorize(Roles = "Owner")]
    public class DeleteModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public DeleteModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AppUser Users { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            var businessId = currentUser?.BusinessId;

            var users = await _context.Users.FirstOrDefaultAsync(m => m.Id == id && m.BusinessId == businessId);

            if (users == null)
            {
                return NotFound();
            }
            else
            {
                Users = users;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            var businessId = currentUser?.BusinessId;

            // Prevent users from deleting themselves
            if (id == currentUserId)
            {
                Users = currentUser!;
                ModelState.AddModelError(string.Empty, "You cannot delete your own account.");
                return Page();
            }

            var users = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.BusinessId == businessId);
            if (users != null)
            {
                _context.Users.Remove(users);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
