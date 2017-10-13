using System;
using System.Collections.Generic;
using Sitecore.Data;

namespace Sitecore.Feature.BulkManager.Core
{
    public class Settings
    {
        public static List<ID> ExcludeTemplates
        {
            get
            {
                var excludeTemplates = new List<ID>();

                string settingValue = Sitecore.Configuration.Settings.GetSetting("BulkManager.ExcludeTemplates", "");

                if (!string.IsNullOrEmpty(settingValue))
                {
                    foreach (string id in settingValue.Split('|'))
                    {
                        ID parsedId;

                        if (ID.TryParse(id, out parsedId))
                        {
                            excludeTemplates.Add(parsedId);
                        }
                    }
                }

                return excludeTemplates;
            }
        }

        public static ID LaunchPadButtonId
        {
            get
            {
                ID launchPadButtonId;

                string settingValue = Sitecore.Configuration.Settings.GetSetting("BulkManager.LaunchPadButtonId", "");

                if (ID.TryParse(settingValue, out launchPadButtonId))
                    return launchPadButtonId;

                return null;
            }
        }
    }
}
