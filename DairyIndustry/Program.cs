using DairyIndustry.Data;
using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using DairyIndustry.Repository;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.Loader;

var builder = WebApplication.CreateBuilder(args);

//  LOAD DLL
var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/lib/libwkhtmltox.dll")
);

//  REGISTER DinkToPdf
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddSingleton<DbHelper>();
builder.Services.AddScoped<ICollectionCenterRepository, CollectionCenterRepository>();
builder.Services.AddScoped<IFarmerRepository, FarmerRepository>();
builder.Services.AddScoped<ActionLogFilter>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ILogisticsRepository, LogisticsRepository>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();