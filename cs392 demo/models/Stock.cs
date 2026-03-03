using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace cs392_demo.models
{
    public class Stock
    {

        [Key]
        [Required]
        [StringLength(50)]
        [Display(Name = "Stock ID")]
        public string Stock_ID { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Location ID")]
        public string Location_Stock_ID { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Item Name")]
        public string Item_Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "SKU")]
        public string SKU { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        public int Amount { get; set; }

        [Display(Name = "Danger Range")]
        public int Danger_Range { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Last Updated")]
        public DateTime? Last_Updated { get; set; }

        [StringLength(256)]
        [Display(Name = "Updated By")]
        public string? Last_Updated_by { get; set; }

        public ICollection<Inventory_Activity_Log> Inventory_Activity_Logs { get; set; } = new List<Inventory_Activity_Log>();


    }
}
