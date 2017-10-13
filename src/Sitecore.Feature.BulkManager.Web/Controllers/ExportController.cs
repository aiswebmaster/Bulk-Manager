using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using System.IO;
using Sitecore.Diagnostics;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Mvc.Controllers;
using Sitecore.Feature.BulkManager.Core.Services;
using CsvHelper;

namespace Sitecore.Feature.BulkManager.Web.Controllers
{
    public class ExportController : Base.BaseController
    {
        [HttpGet]
        public ActionResult ExportQuery(string databaseName, string query, string languages, bool includeStandardFields)
        {
            VerifyImportExportPermissions();

            Assert.IsNotNullOrEmpty(databaseName, "databaseName was not provided");
            Assert.IsNotNullOrEmpty(query, "query was not provided");

            try
            {
                var database = Sitecore.Data.Database.GetDatabase(databaseName);
                DataTable dataTable;

                if (string.IsNullOrEmpty(languages))
                    languages = Sitecore.Context.Language.Name;

                var parsedLanguages = ParseLanguages(languages);
                var exportService = new ExportService(database, parsedLanguages, includeStandardFields);

                dataTable = exportService.RetrieveItemsByQuery(query);

                WriteCsvToStream(Response.OutputStream, dataTable);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Failure while exporting from BulkManager", ex);
                
                return Content(ex.Message);
            }

            return Content("");
        }

        [HttpGet]
        public ActionResult ExportPath(string databaseName, string path, string languages, bool includeStandardFields)
        {
            VerifyImportExportPermissions();

            Assert.IsNotNullOrEmpty(databaseName, "databaseName was not provided");
            Assert.IsNotNullOrEmpty(path, "path was not provided");

            try
            {
                var database = Sitecore.Data.Database.GetDatabase(databaseName);
                DataTable dataTable;

                if (string.IsNullOrEmpty(languages))
                    languages = Sitecore.Context.Language.Name;

                var parsedLanguages = ParseLanguages(languages);
                var exportService = new ExportService(database, parsedLanguages, includeStandardFields);

                Item parentItem = database.GetItem(path);

                Assert.IsNotNull(parentItem, "Could not load parent item");

                dataTable = exportService.RetrieveItemsByParentItem(parentItem);

                WriteCsvToStream(Response.OutputStream, dataTable);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Failure while exporting from BulkManager", ex);

                return Content(ex.Message);
            }

            return Content("");
        }

        private void WriteCsvToStream(Stream stream, DataTable dataTable)
        {
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", String.Format("attachment; filename=ItemExport_{0}.csv", DateTime.Now.ToString("yyyyMMddhhmmss")));

            using (StreamWriter writer = new StreamWriter(stream))
            {
                var csv = new CsvWriter(writer);

                foreach (DataColumn column in dataTable.Columns)
                {
                    csv.WriteField(column.ColumnName);
                }
                csv.NextRecord();

                foreach (DataRow row in dataTable.Rows)
                {
                    for (var i = 0; i < dataTable.Columns.Count; i++)
                    {
                        csv.WriteField(row[i]);
                    }
                    csv.NextRecord();
                }
            }
        }

        private List<Language> ParseLanguages(string languages)
        {
            var parsedLanguages = new List<Language>();

            try
            {
                foreach (var language in languages.Split(','))
                {
                    parsedLanguages.Add(Language.Parse(language));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to parse list of languages", ex);
            }

            return parsedLanguages;
        }

    }
}