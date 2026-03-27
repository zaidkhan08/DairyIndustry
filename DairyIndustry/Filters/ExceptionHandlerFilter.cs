using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using DairyIndustry.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DairyIndustry.Filters
{
    public class ExceptionHandlerFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
            };

            context.Result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<ErrorViewModel>(
                    new EmptyModelMetadataProvider(),
                    context.ModelState
                )
                {
                    Model = model
                }
            };

            context.ExceptionHandled = true;
        }
    }
}