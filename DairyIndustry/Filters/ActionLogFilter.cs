using DairyIndustry.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace DairyIndustry.Filters
{
    public class ActionLogFilter : IActionFilter
    {
        private readonly DbHelper _db;

        public ActionLogFilter(DbHelper db)
        {
            _db = db;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                string action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
                string controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";

                string entityName = $"{controller}/{action}";

                using (SqlConnection conn = _db.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("Admin.usp_Admin_WriteAuditLog", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Action", $"Visited {action}");
                    cmd.Parameters.AddWithValue("@EntityName", entityName);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
