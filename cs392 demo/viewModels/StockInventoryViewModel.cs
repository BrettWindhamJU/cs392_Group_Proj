namespace cs392_demo.viewModels
{
    public class StockInventoryViewModel
    {
        // ---- Stock Properties ----
        public char Stock_ID { get; set; }
        public char Location_Stock_ID { get; set; }
        public string Item_Name { get; set; }
        public char SKU { get; set; }
        public int Amount { get; set; }
        public int Danger_Range { get; set; }
        public DateTime Last_Updated { get; set; }
        public DateTime Last_Updated_by { get; set; }

        // ---- Inventory Location Properties ----
        public string Location_Id { get; set; }
        public string Location_Name { get; set; }
        public string Address_Location { get; set; }
        public string Owner_User_ID { get; set; }
        
    }
}
