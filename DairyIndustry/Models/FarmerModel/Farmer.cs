using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.FarmerModel
{
    public class Farmer
    {
        public int FarmerId { get; set; }
        public string FarmerName{ get; set; }
        public int VillageId{ get; set; }
        public string VillageName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public int BankAccountId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string ProfilePhoto { get; set; }
        [DataType(DataType.Upload)]
        public IFormFile ProfileImageFile { get; set; }
    }
}
