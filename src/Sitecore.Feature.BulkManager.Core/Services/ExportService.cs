using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.Mvc;
using System.IO;
using Sitecore.Globalization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Sitecore.Data.Templates;
using Sitecore.Data.Managers;

namespace Sitecore.Feature.BulkManager.Core.Services
{
    public class ExportService
    {
        private List<ID> _standardTemplateFieldIds;
        private bool _includeStandardFields;
        private List<Language> _languages;
        private Database _database;

        public ExportService(Database database, List<Language> languages, bool includeStandardFields)
        {
            _languages = languages;
            _includeStandardFields = includeStandardFields;
            _database = database;

            //Prepopulate the standard fields for better performance
            Template baseTemplate = TemplateManager.GetTemplate(Sitecore.Configuration.Settings.DefaultBaseTemplate, database);

            _standardTemplateFieldIds = new List<ID>();

            foreach (TemplateField field in baseTemplate.GetFields())
                _standardTemplateFieldIds.Add(field.ID);
        }

        public DataTable RetrieveItemsByQuery(string query)
        {
            var queryItems = _database.SelectItems(query);
            DataTable dataTable;
            
            dataTable = ProcessItemList(queryItems);

            return dataTable;
        }

        public DataTable RetrieveItemsByParentItem(Item parentItem)
        {
            var descendentItems = parentItem.Axes.GetDescendants();
            DataTable dataTable;

            dataTable = ProcessItemList(descendentItems);

            return dataTable;
        }
        
        private DataTable ProcessItemList(Item[] items)
        {
            var dataTable = new DataTable();
            var excludeTemplateId = Settings.ExcludeTemplates;

            //Add fields
            dataTable.Columns.Add(Constants.FIELD_ITEM_ID, typeof(string));
            dataTable.Columns.Add(Constants.FIELD_VERSION, typeof(string));
            dataTable.Columns.Add(Constants.FIELD_LANGUAGE, typeof(string));
            dataTable.Columns.Add(Constants.FIELD_PARENT_ID, typeof(string));

            foreach (Item exportItem in items)
            {
                //Skip item if template is in the excluded list
                if (excludeTemplateId.Contains(exportItem.TemplateID))
                    continue;

                //If there is only one language versions then export. Otherwise we will iterate through the items language list
                if (exportItem.Languages.Count() == 1)
                {
                    AddItemToDataTable(exportItem, dataTable);
                }
                else
                {
                    foreach (var language in exportItem.Languages)
                    {
                        //Skip this language if it was not requested
                        if (!_languages.Contains(language))
                            continue;

                        var itemVersion = exportItem.Database.GetItem(exportItem.ID, language);

                        //Only add to the dataTable if there are language versions for this item
                        if (itemVersion.Versions.Count > 0)
                        {
                            AddItemToDataTable(itemVersion, dataTable);
                        }
                    }
                }
            }

            return dataTable;
        }

        private void AddItemToDataTable(Item exportItem, DataTable dataTable)
        {
            DataRow dataRow = dataTable.NewRow();

            foreach (Field exportField in exportItem.Fields)
            {
                //If not exporting fields and this is a standard template field we should skip this field
                if (!_includeStandardFields && IsStandardTemplateField(exportField))
                    continue;

                //Check if the field exists in the data table. If not, create it
                if (!dataTable.Columns.Contains(exportField.Name))
                    dataTable.Columns.Add(exportField.Name, typeof(string));

                dataRow[exportField.Name] = exportField.Value;
            }

            dataRow[Constants.FIELD_ITEM_ID] = exportItem.ID;
            dataRow[Constants.FIELD_VERSION] = exportItem.Version;
            dataRow[Constants.FIELD_LANGUAGE] = exportItem.Language;
            dataRow[Constants.FIELD_PARENT_ID] = exportItem.ParentID;

            dataTable.Rows.Add(dataRow);
        }

        private bool IsStandardTemplateField(Field field)
        {
            if (_standardTemplateFieldIds.Contains(field.ID))
                return true;

            return false;
        }
    }
}
