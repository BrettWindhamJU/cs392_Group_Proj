using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Stock
    {

        [Key]

        public string Stock_ID { get; set; } = string.Empty;

        public string Location_Stock_ID { get; set; } = string.Empty;


        public string Item_Name { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public int Amount { get; set; }

        public int Danger_Range { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? Last_Updated { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? Last_Updated_by { get; set; }


    }
}
