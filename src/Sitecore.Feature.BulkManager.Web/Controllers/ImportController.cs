using System;
using System.Web.Mvc;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Feature.BulkManager.Core.Models;
using Sitecore.Feature.BulkManager.Core.Services;

namespace Sitecore.Feature.BulkManager.Web.Controllers
{
    public class ImportController : Base.BaseController
    {
        public JsonResult ImportItems(HttpPostedFileBase file, string database, bool versionChecked)
        {
            VerifyImportExportPermissions();

            Assert.IsNotNull(file, "File was not provided");

            var importService = new ImportService(database, versionChecked);
            ImportResult importResult = null;

            try
            {
                importResult = importService.ImportCsv(file.InputStream);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Failure to import CSV", ex);
                throw;
            }

            return Json(importResult, JsonRequestBehavior.AllowGet);
        }
    }
}