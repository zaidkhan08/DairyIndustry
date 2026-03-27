using DairyIndustry.Data;
using DairyIndustry.Repositories;
using DairyIndustry.Repositories.Interfaces;
using DairyIndustry.Repository;

namespace DairyIndustry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ? Add services to the container
            builder.Services.AddControllersWithViews();

            // ? Session services
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
       

            // ? HttpContext (for session)
            builder.Services.AddHttpContextAccessor();


            builder.Services.AddScoped<DbHelper>();

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICollectionCenterRepository, CollectionCenterRepository>();
            builder.Services.AddScoped<IFarmerRepository, FarmerRepository>();
            builder.Services.AddScoped<ILocationRepository,LocationRepository>();

            var app = builder.Build();

            // ? Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ? IMPORTANT: Enable session
            app.UseSession();

            app.UseAuthorization();

            // ? Default route ? Login page first
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Index}/{id?}");

            app.Run();
        }
    }
}