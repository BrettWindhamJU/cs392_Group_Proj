using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Stock
    {

        [Key]

        public char Stock_ID { get; set; }

        public char Location_Stock_ID { get; set; }


        public string Item_Name { get; set; }

        public char SKU { get; set; }

        public int Amount { get; set; }

        public int Danger_Range { get; set; }

        public DateTime Last_Updated { get; set; }

        public DateTime Last_Updated_by { get; set; }


    }
}
