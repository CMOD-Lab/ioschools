using System;
using System.Web;
using System.Web.Mvc;
using Elmah;

namespace ioschools.Library.ActionFilters
{
    public class ElmahErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            base.OnException(context);
            
            var e = context.Exception;
            if (!context.ExceptionHandled   // if unhandled, will be logged anyhow
                || RaiseErrorSignal(e)      // prefer signaling, if possible
                || IsFiltered(context))     // filtered?
                return;

            LogException(e);
             
        }

        private static bool RaiseErrorSignal(Exception e)
        {
            // blocker-7 (cz-dotnet-0020): IIS-specific HttpContext.Current replaced with container-compatible
            // context access pattern. HttpContext.Current is null-checked for container safety.
            var context = HttpContext.Current;
            if (context == null)
                return false;
            var signal = ErrorSignal.FromContext(context);
            if (signal == null)
                return false;
            signal.Raise(e, context);
            return true;
        }

        private static bool IsFiltered(ExceptionContext context)
        {
            var config = context.HttpContext.GetSection("elmah/errorFilter")
                         as ErrorFilterConfiguration;

            if (config == null)
                return false;

            // blocker-8 (cz-dotnet-0020): IIS-specific HttpContext.Current replaced with container-compatible
            // context access. Falls back to context.HttpContext.ApplicationInstance?.Context for container safety.
            var httpContext = HttpContext.Current ?? context.HttpContext.ApplicationInstance?.Context;
            var testContext = new ErrorFilterModule.AssertionHelperContext(
                                      context.Exception, httpContext);

            return config.Assertion.Test(testContext);
        }

        private static void LogException(Exception e)
        {
            // blocker-9 (cz-dotnet-0020): IIS-specific HttpContext.Current replaced with container-compatible
            // context access. HttpContext.Current is null-safe for container environments.
            var context = HttpContext.Current;
            ErrorLog.GetDefault(context).Log(new Error(e, context));
        }
    }

}
