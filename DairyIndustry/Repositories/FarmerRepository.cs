using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

public class FarmerRepository : IFarmerRepository
{
    private readonly DbHelper _dbHelper;

    public FarmerRepository(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

   
}