#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using cs392_demo.models;
using cs392_demo.Constants;
using cs392_demo.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace cs392_demo.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly IUserStore<Users> _userStore;
        private readonly IUserEmailStore<Users> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly cs392_demoContext _dbContext;

        public RegisterModel(
            UserManager<Users> userManager,
            IUserStore<Users> userStore,
            SignInManager<Users> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            cs392_demoContext dbContext)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>True when the page was opened via a manager invite link.</summary>
        public bool IsManagerInvite { get; set; }

        /// <summary>Business name shown on the invite banner.</summary>
        public string InvitedBusinessName { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>Self-registration role: Owner or User only. Manager must use an invite link.</summary>
            public string Role { get; set; }

            /// <summary>Required when Role = Owner. Name of the new business to create.</summary>
            [Display(Name = "Business Name")]
            public string BusinessName { get; set; }

            /// <summary>Required when Role = User. Invite code provided by the business owner.</summary>
            [Display(Name = "Business Invite Code")]
            public string InviteCode { get; set; }

            /// <summary>Populated from ?inviteToken= query string for manager registrations.</summary>
            public string InviteToken { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string inviteToken = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!string.IsNullOrEmpty(inviteToken))
            {
                var invite = await _dbContext.ManagerInvitation
                    .Include(i => i.Business)
                    .FirstOrDefaultAsync(i => i.Token == inviteToken && !i.IsUsed && i.ExpiresAt > DateTime.UtcNow);

                if (invite == null)
                {
                    // Invalid / expired token - show a friendly error
                    ModelState.AddModelError(string.Empty, "This invitation link is invalid or has expired. Please ask your owner to send a new one.");
                    return Page();
                }

                var staffUser = await _userManager.FindByEmailAsync(invite.Email);
                if (staffUser == null || staffUser.BusinessId != invite.BusinessId)
                {
                    ModelState.AddModelError(string.Empty,
                        "This manager invitation is for an existing staff account, but no matching staff user was found for this business. Please contact your boss.");
                    return Page();
                }

                IsManagerInvite = true;
                InvitedBusinessName = invite.Business?.Business_Name ?? string.Empty;
                Input = new InputModel
                {
                    Email = invite.Email,
                    InviteToken = inviteToken,
                    Role = "Manager"
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ── Manager invite path ──────────────────────────────────────────────
            if (!string.IsNullOrEmpty(Input.InviteToken))
            {
                IsManagerInvite = true;
                var invite = await _dbContext.ManagerInvitation
                    .Include(i => i.Business)
                    .FirstOrDefaultAsync(i =>
                        i.Token == Input.InviteToken &&
                        !i.IsUsed &&
                        i.ExpiresAt > DateTime.UtcNow);

                if (invite == null)
                {
                    ModelState.AddModelError(string.Empty, "This invitation link is invalid or has expired.");
                    return Page();
                }

                if (!string.Equals(invite.Email, Input.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Input.Email", "The email address must match the one this invitation was sent to.");
                    return Page();
                }

                // Manager invites promote existing staff accounts; no new account creation here.
                ModelState.Remove("Input.ConfirmPassword");

                var existingUser = await _userManager.FindByEmailAsync(Input.Email.Trim());
                if (existingUser == null || existingUser.BusinessId != invite.BusinessId)
                {
                    ModelState.AddModelError(string.Empty,
                        "No existing staff account was found for this email in the invited business.");
                    return Page();
                }

                var validPassword = await _userManager.CheckPasswordAsync(existingUser, Input.Password);
                if (!validPassword)
                {
                    ModelState.AddModelError("Input.Password", "Incorrect password. Sign in with your existing staff password to accept this invitation.");
                    return Page();
                }

                if (!ModelState.IsValid)
                    return Page();

                if (!await _userManager.IsInRoleAsync(existingUser, Roles.Manager.ToString()))
                {
                    await _userManager.AddToRoleAsync(existingUser, Roles.Manager.ToString());
                }

                // Once the promotion is accepted, invalidate every outstanding invite for this user in the same business.
                var matchingInvites = await _dbContext.ManagerInvitation
                    .Where(i =>
                        i.BusinessId == invite.BusinessId &&
                        i.Email == invite.Email &&
                        !i.IsUsed)
                    .ToListAsync();

                foreach (var matchingInvite in matchingInvites)
                {
                    matchingInvite.IsUsed = true;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Existing staff account promoted to Manager via invitation.");

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // ── Self-registration path (Owner or User only) ──────────────────────
            if (Input.Role == Roles.Manager.ToString())
            {
                ModelState.AddModelError(string.Empty, "Manager accounts can only be created via an owner invitation link.");
                return Page();
            }

            if (Input.Role != Roles.User.ToString() && Input.Role != Roles.Owner.ToString())
            {
                ModelState.AddModelError(string.Empty, "Invalid role selected.");
                return Page();
            }

            // Role specific field validation
            if (Input.Role == Roles.Owner.ToString())
            {
                if (string.IsNullOrWhiteSpace(Input.BusinessName))
                    ModelState.AddModelError("Input.BusinessName", "Business name is required when registering as an Owner.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.InviteCode))
                    ModelState.AddModelError("Input.InviteCode", "An invite code is required to join a business as a Staff member.");
            }

            if (!ModelState.IsValid)
                return Page();

            // Look up or create business before creating the user
            string businessId = null;

            if (Input.Role == Roles.Owner.ToString())
            {
                var newBusiness = new Business
                {
                    Business_ID = "BIZ-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    Business_Name = Input.BusinessName.Trim(),
                    Invite_Code = GenerateInviteCode()
                };
                _dbContext.Business.Add(newBusiness);
                await _dbContext.SaveChangesAsync();
                businessId = newBusiness.Business_ID;
            }
            else
            {
                var business = await _dbContext.Business
                    .FirstOrDefaultAsync(b => b.Invite_Code == Input.InviteCode.Trim().ToUpper());

                if (business == null)
                {
                    ModelState.AddModelError("Input.InviteCode", "No business found with that invite code. Please check the code and try again.");
                    return Page();
                }
                businessId = business.Business_ID;
            }

            var user = CreateUser();
            user.BusinessId = businessId;

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _userManager.AddToRoleAsync(user, Input.Role);

                var userId = await _userManager.GetUserIdAsync(user);

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    null,
                    new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, false);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private static string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // avoid ambiguous chars
            var rng = new Random();
            return new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        private Users CreateUser()
        {
            return Activator.CreateInstance<Users>();
        }


        private IUserEmailStore<Users> GetEmailStore()
        {
            return (IUserEmailStore<Users>)_userStore;
        }
    }
}