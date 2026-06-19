using DairyIndustry.Data;
using DairyIndustry.Filters;
using DairyIndustry.Interfaces;
using DairyIndustry.Models.Admin;
using DairyIndustry.Repositories;
using DairyIndustry.Repository;
using DairyIndustry.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Stripe;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Http.Features;

namespace DairyIndustry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<DbHelper>();
            builder.Services.AddScoped<ActionLogFilter>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ILogisticsRepository, LogisticsRepository>();
            builder.Services.AddScoped<IProductionRepository, ProductionRepository>();
            builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<ICollectionCenterRepository, CollectionCenterRepository>();
            builder.Services.AddScoped<IFarmerRepository, FarmerRepository>();
            builder.Services.AddScoped<IHomeRepository, HomeRepository>();
            builder.Services.AddScoped<FileUploadService>();
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IChillingCenterRepository, ChillingCenterRepository>();
            builder.Services.AddScoped<IHRRepository, HRRepository>();
            builder.Services.AddScoped<ISalesRepository, SalesRepository>();


            //  REGISTER DinkToPdf
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();

            builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));


            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ResultInfoFilter>();
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });


            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5MB
            });
            builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
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
                pattern: "{controller=Sales}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
