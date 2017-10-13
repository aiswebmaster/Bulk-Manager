using System;
using Sitecore.Mvc.Controllers;
using Sitecore.Data;
using Sitecore.Feature.BulkManager.Core;

namespace Sitecore.Feature.BulkManager.Web.Controllers.Base
{
    public class BaseController : SitecoreController
    {

        protected void VerifyImportExportPermissions()
        {
            var coreDb = Database.GetDatabase("core");
            ID securityItemId = Settings.LaunchPadButtonId;

            if(ID.IsNullOrEmpty(securityItemId) || coreDb.GetItem(securityItemId) == null)
            {
                throw new UnauthorizedAccessException("User does not have access to Data Manager");
            }
        }
    }
}