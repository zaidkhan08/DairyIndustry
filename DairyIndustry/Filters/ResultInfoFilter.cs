using Microsoft.AspNetCore.Mvc.Filters;

namespace DairyIndustry.Filters
{
    public class ResultInfoFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add("Application-Version", "1.0");
            context.HttpContext.Response.Headers.Add("Application-Name", "DairyIndustry");
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}