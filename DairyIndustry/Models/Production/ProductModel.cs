namespace DairyIndustry.Models.Production
{
    public class ProductModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal MRP { get; set; }
        public string Unit { get; set; }
        public int? ShelfLifeDays { get; set; }
    }
}