using System;
using System.IO;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Feature.BulkManager.Core.Models;
using CsvHelper;
using Sitecore.Globalization;
using Sitecore.SecurityModel;

namespace Sitecore.Feature.BulkManager.Core.Services
{
    public class ImportService
    {
        public ImportService() : this("master", false)
        { }

        public ImportService(string database, bool createNewVersion)
        {
            WorkingDatabase = Sitecore.Configuration.Factory.GetDatabase(database);
            CreateNewVersion = createNewVersion;
        }

        public ImportResult ImportCsv(Stream stream)
        {
            var importResult = new ImportResult();

            using (var streamReader = new System.IO.StreamReader(stream))
            {
                var csvReader = new CsvReader(streamReader);

                csvReader.Configuration.HasHeaderRecord = true;

                //csvReader.Read() acts like a move next. Will return false when at the end of the file
                while (csvReader.Read())
                {
                    var importItemResult = new ImportItemResult();

                    try
                    {
                        Item item = null;
                        ID itemId = null;

                        if (csvReader.FieldHeaders.Contains(Constants.FIELD_ITEM_ID))
                        {
                            itemId = ParseIdOrError(csvReader, Constants.FIELD_ITEM_ID);
                            importItemResult.ItemId = itemId;
                        }

                        Language language = null;
                        Data.Version version = null;

                        if(csvReader.FieldHeaders.Contains(Constants.FIELD_LANGUAGE))
                            language = ParseLanguageOrError(csvReader, Constants.FIELD_LANGUAGE);

                        if (csvReader.FieldHeaders.Contains(Constants.FIELD_VERSION))
                            version = ParseVersionOrError(csvReader, Constants.FIELD_VERSION);

                        if (!ID.IsNullOrEmpty(itemId)) {

                            item = GetItem(itemId, language, version);

                            //The user might not be able to access this item. We need to make sure the item dosent exist yet
                            if (item == null)
                            {
                                using (new SecurityDisabler())
                                {
                                    item = GetItem(itemId, language, version);

                                    Assert.IsNull(item, String.Format("User does not have access to the item with ID {0}", itemId));
                                }
                            }

                            Assert.IsNotNull(item, String.Format("Could not find item with ID {0}", itemId));
                        }

                        importItemResult.IsCreation = item == null;

                        if (importItemResult.IsCreation)
                        {
                            CreateItem(csvReader, language, importItemResult);
                        }
                        else
                        {
                            UpdateItem(csvReader, item, importItemResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        importItemResult.Success = false;
                        importItemResult.ErrorMessage = ex.Message;

                        Sitecore.Diagnostics.Log.Error("BulkManager import process failed for item", ex, this);
                    }
                    finally
                    {
                        importResult.ItemResults.Add(importItemResult);

                        //Increment metrics
                        if (importItemResult.Success && importItemResult.IsCreation)
                            importResult.ItemsCreated++;
                        else if(importItemResult.Success && !importItemResult.IsCreation)
                            importResult.ItemsUpdated++;
                        else if (!importItemResult.Success)
                            importResult.ItemsFailed++;

                    }
                }
            }

            return importResult;
        }

        public bool CreateItem(CsvReader csv, Language language, ImportItemResult importItemResult)
        {
            Item parentItem = null;
            TemplateItem template = null;
            string newItemName = string.Empty;

            //Make sure we are dealing with a valid row
            if (csv == null || csv.FieldHeaders == null || !csv.FieldHeaders.Any() || csv.CurrentRecord == null || !csv.CurrentRecord.Any())
            {
                CreateErrorResponse(importItemResult, String.Format("Could not create item, in row {0}, row was empty", csv.Row.ToString()));
                return false;
            }

            foreach (var header in csv.FieldHeaders)
            {
                switch (header)
                {
                    case Constants.FIELD_PARENT_ID:

                        ID itemId = ParseIdOrError(csv, header);

                        if (language == null)
                            parentItem = WorkingDatabase.GetItem(itemId);
                        else
                            parentItem = WorkingDatabase.GetItem(itemId, language);

                        Assert.IsNotNull(parentItem, "ParentItem {0} not found or not accessible", itemId);

                        break;
                    case Constants.FIELD_ITEM_NAME:

                        newItemName = csv.GetField<string>(header);
                        importItemResult.ItemName = newItemName;

                        break;
                    case Constants.FIELD_TEMPLATE_ID:

                        ID templateId = ParseIdOrError(csv, header);
                        template = WorkingDatabase.GetTemplate(templateId);
                        Assert.IsNotNull(template, "Template {0} not found or not accessible", templateId);

                        break;
                }
            }

            try
            {
                Item newItem = parentItem.Add(newItemName, template);

                if (newItem == null)
                {
                    CreateErrorResponse(importItemResult, String.Format("Error when creating item {0}", newItemName));
                    return false;
                }

                importItemResult.ItemId = newItem.ID;

                return UpdateItem(csv, newItem, importItemResult);
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is InvalidOperationException)
                {
                    CreateErrorResponse(importItemResult, ex.Message);
                    return false;
                }
                else
                {
                    throw new Exception(String.Format("A fatal error occured while creating item {0}", newItemName), ex.InnerException);
                }
            }
        }

        public bool UpdateItem(CsvReader csv, Item item, ImportItemResult importItemResult)
        {
            if (csv == null || csv.FieldHeaders == null || !csv.FieldHeaders.Any() || csv.CurrentRecord == null || !csv.CurrentRecord.Any())
            {
                CreateErrorResponse(importItemResult, String.Format("Could not create item, in row {0}, row was empty", csv.Row.ToString()));
                return false;
            }

            //Ensure all fields are available
            item.Fields.ReadAll();

            try
            {

                //If this language does not have any versions then we need to create one
                if (item.Versions.Count == 0 || (CreateNewVersion && !importItemResult.IsCreation))
                    item = item.Versions.AddVersion();

                item.Editing.BeginEdit();

            try
            {
                //Iterate through each filed in the CSV and see if we should set this item
                foreach (var header in csv.FieldHeaders)
                {
                    if (header.Equals(Constants.FIELD_ITEM_NAME, StringComparison.InvariantCultureIgnoreCase))
                    {
                        item.Name = csv.GetField<string>(header);
                    }
                    else
                    {
                        //TODO: This could probably be done better
                        var itemField = item.Fields.FirstOrDefault(x => x.Name.Equals(header, StringComparison.InvariantCultureIgnoreCase));

                        if (itemField != null)
                            itemField.Value = csv.GetField<string>(header);
                    }
                }

                    importItemResult.Success = true;
                }
                catch (Exception ex)
                {
                    CreateErrorResponse(importItemResult, ex.Message);
                    return false;
                }
                finally
                {
                    item.Editing.EndEdit();
                }

            }
            catch (Exception ex)
            {
                CreateErrorResponse(importItemResult, ex.Message);
                return false;
            }

            return true;
        }

        private Item GetItem(ID itemId, Language language, Data.Version version)
        {
            Item item;

            if (language == null && version == null)
            {
                item = WorkingDatabase.GetItem(itemId);
            }
            else if (language == null && version != null)
            {
                item = WorkingDatabase.GetItem(itemId,Sitecore.Context.Language, version);
            }
            else if (language != null && version == null)
            {
                item = WorkingDatabase.GetItem(itemId, language);
            }
            else
            {
                item = WorkingDatabase.GetItem(itemId, language, version);
            }

            return item;
        }

        private ID ParseIdOrError(CsvReader csv, string headerName)
        {
            string id = csv.GetField<string>(headerName);

            if (string.IsNullOrEmpty(id))
                return null;

            ID parsedId;

            if (ID.TryParse(id, out parsedId))
                return parsedId;
            else
                throw new FormatException(String.Format("Could not parse field {0} for value {1}", headerName, id));
        }

        private Language ParseLanguageOrError(CsvReader csv, string headerName)
        {
            string language = csv.GetField<string>(headerName);

            if (string.IsNullOrEmpty(language))
                return null;

            Language parsedLanguage;

            if (Language.TryParse(language, out parsedLanguage))
                return parsedLanguage;
            else
                throw new FormatException(String.Format("Could not parse field {0} for value {1}", headerName, language));
        }

        private Data.Version ParseVersionOrError(CsvReader csv, string headerName)
        {
            string version = csv.GetField<string>(headerName);

            if (string.IsNullOrEmpty(version))
                return null;

            Data.Version parsedVersion;

            if (Data.Version.TryParse(version, out parsedVersion))
                return parsedVersion;
            else
                throw new FormatException(String.Format("Could not parse field {0} for value {1}", headerName, version));
        }

        private ImportItemResult CreateErrorResponse(ImportItemResult importItemResult, string errorMessage)
        {
            importItemResult.Success = false;
            importItemResult.ErrorMessage = errorMessage;

            return importItemResult;
        }

        private Database WorkingDatabase { get; set; }
        private bool CreateNewVersion { get; set; }
    }
}
