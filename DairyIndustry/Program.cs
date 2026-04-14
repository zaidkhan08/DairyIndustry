using DairyIndustry.Data;
//Updated upstream
using DairyIndustry.Filters;

//Stashed changes
using DairyIndustry.Repositories;

namespace DairyIndustry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

        // Updated upstream
            builder.Services.AddSingleton<DbHelper>();
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

            

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<DbHelper>();
            builder.Services.AddScoped<IChillingCenterRepository, ChillingCenterRepository>();
            builder.Services.AddScoped<IHRRepository, HRRepository>();
            builder.Services.AddScoped<ISalesRepository, SalesRepository>();

            // Stashed changes

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