using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class Inventory_Location
    {
        [Key]
        public int Location_Key { get; set; }

        [Display(Name = "Location ID")]
        [Required]
        public string location_id { get; set; } = string.Empty;

        [Display(Name = "Location Name")]
        public string Location_name { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address_Location { get; set; } = string.Empty;

        [Display(Name = "Owner User ID")]
        public string Owner_User_ID { get; set; } = string.Empty;

        /// <summary>The business this location belongs to.</summary>
        public string? BusinessId { get; set; }

        [ForeignKey(nameof(BusinessId))]
        public Business? Business { get; set; }

        [NotMapped]
        public string Location_ID
        {
            get => location_id;
            set => location_id = value;
        }

        [NotMapped]
        public string Location_Name
        {
            get => Location_name;
            set => Location_name = value;
        }

    }
}
