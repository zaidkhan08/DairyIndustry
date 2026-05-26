using DairyIndustry.Data;
using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using DairyIndustry.Repository;
using DairyIndustry.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Stripe;
using System.Runtime.Loader;
namespace DairyIndustry
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //  LOAD DLL
            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/lib/libwkhtmltox.dll")
            );


            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<DbHelper>();
            //builder.Services.AddControllersWithViews(options =>
            builder.Services.AddScoped<ActionLogFilter>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ILogisticsRepository, LogisticsRepository>();
            builder.Services.AddScoped<IProductionRepository, ProductionRepository>();
            builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<ICollectionCenterRepository, CollectionCenterRepository>();
            builder.Services.AddScoped<IFarmerRepository, FarmerRepository>();
            builder.Services.AddScoped<FileUploadService>();


            //  REGISTER DinkToPdf
            builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));


            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

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
