namespace DairyIndustry.Models.Admin
{
    public class ProductModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal MRP { get; set; }
        public string Unit { get; set; }
        public int? ShelfLifeDays { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; }   
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public string ModifiedByName { get; set; }  
    }
}
