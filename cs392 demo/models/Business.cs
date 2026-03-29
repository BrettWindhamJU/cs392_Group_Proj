using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Business
    {
        [Key]
        public string Business_ID { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Business Name")]
        public string Business_Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// Unique code staff/managers enter at registration to join this business.
        [Display(Name = "Invite Code")]
        public string Invite_Code { get; set; } = string.Empty;

        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Inventory_Location> Locations { get; set; } = new List<Inventory_Location>();
    }
}
