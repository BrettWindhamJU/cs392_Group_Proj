using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class ManagerInvitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BusinessId { get; set; } = string.Empty;

        [ForeignKey(nameof(BusinessId))]
        public Business? Business { get; set; }

        /// <summary>The email address this invitation was sent to.</summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Unique single-use token embedded in the registration link.</summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Invitations expire after 7 days by default.</summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    }
}
