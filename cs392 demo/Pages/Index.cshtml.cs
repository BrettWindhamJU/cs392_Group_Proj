using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class IndexModel : PageModel
{
    private readonly cs392_demoContext _context;

    public int TotalStockItems { get; private set; }
    public int LowStockItems { get; private set; }
    public int TotalLogs { get; private set; }
    public int TotalLocations { get; private set; }
    public string? BusinessName { get; private set; }
    public string? StaffInviteCode { get; private set; }
    public string? PendingManagerInviteLink { get; private set; }
    public IList<Inventory_Activity_Log> RecentLogs { get; private set; } = new List<Inventory_Activity_Log>();

    public IndexModel(cs392_demoContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                // No business linked — show zeros
                return;
            }

            var business = await _context.Business.FirstOrDefaultAsync(b => b.Business_ID == businessId);
            BusinessName = business?.Business_Name;
            StaffInviteCode = business?.Invite_Code;

            var currentEmail = (currentUser?.Email ?? string.Empty).Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(currentEmail))
            {
                var inviteToken = await _context.ManagerInvitation
                    .Where(i => i.BusinessId == businessId
                                && i.Email == currentEmail
                                && !i.IsUsed
                                && i.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => i.Token)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(inviteToken))
                {
                    PendingManagerInviteLink = $"/Identity/Account/Register?inviteToken={inviteToken}";
                }
            }

            TotalLocations = await _context.Inventory_Location
                .CountAsync(l => l.BusinessId == businessId);

            TotalStockItems = await _context.Stock
                .CountAsync(s => s.BusinessId == businessId);

            LowStockItems = await _context.Stock
                .CountAsync(s => s.BusinessId == businessId && s.Amount <= s.Danger_Range);

            TotalLogs = await _context.Inventory_Activity_Log
                .CountAsync(l => l.BusinessId == businessId);

            RecentLogs = await _context.Inventory_Activity_Log
                .Where(l => l.BusinessId == businessId)
                .OrderByDescending(log => log.Changed_At)
                .Take(5)
                .ToListAsync();
        }
        catch (Exception)
        {
            // Keep dashboard resilient if the DB is temporarily unavailable.
            TotalStockItems = 0;
            LowStockItems = 0;
            TotalLogs = 0;
            TotalLocations = 0;
            PendingManagerInviteLink = null;
            RecentLogs = new List<Inventory_Activity_Log>();
        }
    }
}