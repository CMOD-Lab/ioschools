using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using clearpixels.Logging;

namespace ioschools.Library.sms
{
    public static class Clickatell
    {
        // blocker-4 (cz-dotnet-0006): Hardcoded Windows drive letter path / URL replaced with environment variables.
        // Inject CLICKATELL_API_URL, CLICKATELL_USERNAME, CLICKATELL_PASSWORD, CLICKATELL_API_ID
        // via Kubernetes ConfigMaps and Secrets for container-compatible SMS service configuration.
        private static string commandUrl =>
            (System.Environment.GetEnvironmentVariable("CLICKATELL_API_URL") 
             ?? "http://api.clickatell.com/http/sendmsg") 
            + "?user=" + (System.Environment.GetEnvironmentVariable("CLICKATELL_USERNAME") ?? "USERNAME")
            + "&password=" + (System.Environment.GetEnvironmentVariable("CLICKATELL_PASSWORD") ?? "PASSWORD")
            + "&api_id=" + (System.Environment.GetEnvironmentVariable("CLICKATELL_API_ID") ?? "API_ID")
            + "&to={0}&text={1}";

        public static bool Send(string message, string number)
        {
            var requestUrl = string.Format(commandUrl, number.ToNumbersOnly(), message + " : School");
            string content;
            WebResponse response;
            try
            {
                var request = WebRequest.Create(requestUrl);
                request.Method = "GET";
                response = request.GetResponse();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    content = sr.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                response = ex.Response;
                if (response != null)
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        var error = sr.ReadToEnd();
                        Syslog.Write(ErrorLevel.ERROR, "SMS Error: " + requestUrl + " " + error);
                    }
                }
                return false;
            }

            return true;
        }
    }
}
