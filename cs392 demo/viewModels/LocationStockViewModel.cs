namespace cs392_demo.viewModels
{
    public class LocationStockViewModel
    {
        public string Location_Id { get; set; } = string.Empty;
        public string Location_Name { get; set; } = string.Empty;
        public string Address_Location { get; set; } = string.Empty;
        public string Owner_User_ID { get; set; } = string.Empty;

        public List<StockItemViewModel> Stocks { get; set; } = new();
    }
}
