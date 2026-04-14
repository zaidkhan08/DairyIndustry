using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models
{
    // Create new order form — Distributor + Plant + Date
    public class SalesOrderFormModel
    {
        [Required(ErrorMessage = "Please select a distributor.")]
        [Display(Name = "Distributor")]
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Please select a plant.")]
        [Display(Name = "Processing Plant")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "Order date is required.")]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;
    }

    // Add line item form on the order details page
    public class AddOrderDetailFormModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Please select a product.")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0.01, 99999.99, ErrorMessage = "Quantity must be greater than 0.")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required.")]
        [Range(0.01, 99999.99, ErrorMessage = "Price must be greater than 0.")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }
    }
}