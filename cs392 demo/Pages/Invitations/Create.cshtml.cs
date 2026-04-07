using cs392_demo.Data;
using cs392_demo.Constants;
using cs392_demo.models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace cs392_demo.Pages.Invitations
{
    [Authorize(Roles = "Owner")]
    public class CreateModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly UserManager<Users> _userManager;

        public CreateModel(cs392_demoContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Staff Member's Email")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.BusinessId == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not linked to a business.");
                return Page();
            }

            var email = Input.Email.Trim().ToLower();

            // Verify the email belongs to an existing staff member in this business
            var staffMember = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.BusinessId == currentUser.BusinessId);

            if (staffMember == null)
            {
                ModelState.AddModelError("Input.Email",
                    "This email does not belong to a staff member in your business. Only existing staff can be promoted to managers.");
                return Page();
            }

            if (await _userManager.IsInRoleAsync(staffMember, Roles.Manager.ToString()))
            {
                ModelState.AddModelError("Input.Email",
                    "That staff member is already a manager for this business.");
                return Page();
            }

            // Check if there's already a pending (unused, non-expired) invite for this email in this business
            var existing = await _context.ManagerInvitation.FirstOrDefaultAsync(i =>
                i.BusinessId == currentUser.BusinessId &&
                i.Email == email &&
                !i.IsUsed &&
                i.ExpiresAt > DateTime.UtcNow);

            if (existing != null)
            {
                ModelState.AddModelError("Input.Email",
                    "A pending invitation already exists for that email address. It will expire on " +
                    existing.ExpiresAt.ToLocalTime().ToString("M/d/yyyy") + ".");
                return Page();
            }

            var invitation = new ManagerInvitation
            {
                BusinessId = currentUser.BusinessId,
                Email = email,
                Token = GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.ManagerInvitation.Add(invitation);
            await _context.SaveChangesAsync();

            TempData["NewInviteToken"] = invitation.Token;
            return RedirectToPage("Index");
        }

        private static string GenerateToken()
        {
            // 32-char URL-safe random token
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "")
                + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
