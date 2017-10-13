using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace Sitecore.Feature.BulkManager.Core.Pipelines
{
    public class InitializeRoutes
    {
        public virtual void Process(PipelineArgs args)
        {
            this.RegisterRoutes(RouteTable.Routes);
        }

        public virtual void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("ExportQueryEndpoint", "api/sitecore/Export/ExportQuery", new { controller = "ExportController", action = "ExportQuery", DisableAnalyticsPageViewsForRequest = true });
            routes.MapRoute("ExportPathEndpoint", "api/sitecore/Export/ExportPath", new { controll = "ExportController", action = "ExportPath", DisableAnalyticsPageViewsForRequest = true});
            routes.MapRoute("ImportItemsEndpoint", "api/sitecore/Import/ImportItems", new { controller = "ImportController", action = "ImportItems", DisableAnalyticsPageViewsForRequest = true });
        }
    }
}
