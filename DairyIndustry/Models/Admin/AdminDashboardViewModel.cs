namespace DairyIndustry.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<StaffModel> Staff { get; set; } = new();
        public List<PlantModel> Plants { get; set; } = new();
        public List<CollectionCenterModel> Centers { get; set; } = new();
        public List<ProductModel> Products { get; set; } = new();
        public List<ProductionBatchModel> Batches { get; set; } = new();
        public List<MilkTransferModel> Transfers { get; set; } = new();
    }
}
