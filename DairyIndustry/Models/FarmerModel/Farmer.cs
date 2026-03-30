using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.FarmerModel
{
    public class Farmer
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }

    }
}