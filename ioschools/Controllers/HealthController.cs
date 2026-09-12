using System;
using System.Web.Mvc;

namespace ioschools.Controllers
{
    /// <summary>
    /// Health check endpoint for container liveness and readiness probes.
    /// Mandatory containerization requirement for Kubernetes/EKS deployments.
    /// </summary>
    public class HealthController : Controller
    {
        // GET /health
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("o"),
                application = "ioschools",
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
            };

            return Json(health, JsonRequestBehavior.AllowGet);
        }
    }
}
