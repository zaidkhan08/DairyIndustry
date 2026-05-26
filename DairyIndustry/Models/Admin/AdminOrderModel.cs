using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Admin
{
    public class AdminOrderModel
    {
        [Required(ErrorMessage = "Please select a distributor")]
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Please select a plant")]
        public int PlantId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Cart items — populated by JS hidden fields before POST
        public List<CartItemModel> CartItems { get; set; } = new();

        // Dropdowns — keep List<T> as you had (no SelectListItem change needed)
        public List<Distributor> DistributorList { get; set; } = new();
        public List<ProductModel> ProductList { get; set; } = new();
        public List<PlantModel> PlantList { get; set; } = new();
    }

    public class CartItemModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}