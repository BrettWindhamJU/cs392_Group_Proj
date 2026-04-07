using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class Inventory_Activity_Log
    {
        [Key]
        [Display(Name = "Log ID")]
        [Required]
        [StringLength(50)]
        public string Log_ID { get; set; } = string.Empty;

        [Display(Name = "Stock ID")]
        [Required]
        [StringLength(50)]
        public string Stock_ID_Log { get; set; } = string.Empty;

        public string? BusinessId { get; set; }

        [ForeignKey(nameof(BusinessId))]
        public Business? Business { get; set; }

        [Display(Name = "Quantity Before")]
        public int Quantity_Before { get; set; }

        [Display(Name = "Quantity After")]
        public int Quantity_After { get; set; }

        [Display(Name = "Changed By")]
        [Required]
        [StringLength(256)]
        public string Changed_By { get; set; } = string.Empty;

        [Display(Name = "Changed At")]
        public DateTime Changed_At { get; set; }


    }
}
