using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Admin
{
    public class AdminOrderModel
    {
        [Required(ErrorMessage = "Please select a distributor")]
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Please select a product")]
        public int ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public List<Distributor> DistributorList { get; set; } = new();
        public List<ProductModel> ProductList { get; set; } = new();
    }
}