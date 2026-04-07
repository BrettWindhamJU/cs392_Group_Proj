using cs392_demo.Data;
using cs392_demo.Constants;
using cs392_demo.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace cs392_demo.Pages.Invitations
{
    [Authorize(Roles = "Owner")]
    public class IndexModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly UserManager<Users> _userManager;

        public IndexModel(cs392_demoContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<ManagerInvitation> Invitations { get; set; } = new List<ManagerInvitation>();
        public string? BusinessName { get; set; }
        public string? StaffInviteCode { get; set; }

        /// <summary>Set after a successful Create redirect so we can show the invite link.</summary>
        [TempData]
        public string NewInviteToken { get; set; } = string.Empty;

        [TempData]
        public string InviteNotice { get; set; } = string.Empty;

        [TempData]
        public string ErrorNotice { get; set; } = string.Empty;

        public string? NewInviteLink { get; set; }

        public async Task<IActionResult> OnPostRegenerateAsync(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser?.BusinessId == null)
                    return RedirectToPage("/Index");

                var invite = await _context.ManagerInvitation
                    .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == currentUser.BusinessId);

                if (invite == null || invite.IsUsed)
                {
                    return RedirectToPage();
                }

                var invitedUser = await _userManager.FindByEmailAsync(invite.Email);
                if (invitedUser != null &&
                    invitedUser.BusinessId == currentUser.BusinessId &&
                    await _userManager.IsInRoleAsync(invitedUser, Roles.Manager.ToString()))
                {
                    invite.IsUsed = true;
                    await _context.SaveChangesAsync();

                    ErrorNotice = "That user is already a manager, so this invitation link is no longer valid.";
                    return RedirectToPage();
                }

                invite.Token = GenerateToken();
                invite.CreatedAt = DateTime.UtcNow;
                invite.ExpiresAt = DateTime.UtcNow.AddDays(7);

                await _context.SaveChangesAsync();

                NewInviteToken = invite.Token;
                InviteNotice = "Invitation link regenerated. Share the updated link with the manager.";
            }
            catch (Exception)
            {
                ErrorNotice = "We couldn't regenerate the invitation right now due to a temporary database connection issue. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser?.BusinessId == null)
                    return RedirectToPage("/Index");

                var business = await _context.Business
                    .FirstOrDefaultAsync(b => b.Business_ID == currentUser.BusinessId);
                BusinessName = business?.Business_Name;
                StaffInviteCode = business?.Invite_Code;

                await MarkAcceptedInvitesAsync(currentUser.BusinessId);

                Invitations = await _context.ManagerInvitation
                    .Where(i => i.BusinessId == currentUser.BusinessId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                // Build the invite link if a token was just created
                if (!string.IsNullOrEmpty(NewInviteToken))
                {
                    NewInviteLink = $"{Request.Scheme}://{Request.Host}/Identity/Account/Register?inviteToken={NewInviteToken}";
                }
            }
            catch (Exception)
            {
                ErrorNotice = "We couldn't load Team Access right now due to a temporary database connection issue. Please refresh in a moment.";
                Invitations = new List<ManagerInvitation>();
            }

            return Page();
        }

        private async Task MarkAcceptedInvitesAsync(string businessId)
        {
            var activeInvites = await _context.ManagerInvitation
                .Where(i => i.BusinessId == businessId && !i.IsUsed)
                .ToListAsync();

            if (activeInvites.Count == 0)
            {
                return;
            }

            var changed = false;

            foreach (var invite in activeInvites)
            {
                var invitedUser = await _userManager.FindByEmailAsync(invite.Email);
                if (invitedUser == null || invitedUser.BusinessId != businessId)
                {
                    continue;
                }

                if (await _userManager.IsInRoleAsync(invitedUser, Roles.Manager.ToString()))
                {
                    invite.IsUsed = true;
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }

        private static string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "")
                + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
