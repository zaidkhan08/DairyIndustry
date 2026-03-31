using DairyIndustry.Controllers;
using DairyIndustry.Data;
using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using DairyIndustry.Repositories.Interfaces;
using DairyIndustry.Repository;
using Microsoft.AspNetCore.Identity.Data;

namespace DairyIndustry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<DbHelper>();
            
            builder.Services.AddScoped<ICollectionCenterRepository, CollectionCenterRepository>();
            builder.Services.AddScoped<IFarmerRepository, FarmerRepository>();
            //builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ActionLogFilter>();       
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ILogisticsRepository,LogisticsRepository>();
            builder.Services.AddControllersWithViews(options =>  
            {
                options.Filters.Add<ExceptionHandlerFilter>();
                options.Filters.Add<ResultInfoFilter>();
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Admin}/{action=Login}/{id?}");

            app.Run();
        }
    }
}