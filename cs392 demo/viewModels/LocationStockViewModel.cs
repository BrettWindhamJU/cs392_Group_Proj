namespace cs392_demo.viewModels
{
    public class LocationStockViewModel
    {
        public string Location_Id { get; set; }
        public string Location_Name { get; set; }
        public string Address_Location { get; set; }
        public string Owner_User_ID { get; set; }

        public List<StockItemViewModel> Stocks { get; set; }
    }
}
