using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;

namespace Sitecore.Feature.BulkManager.Core.Models
{
    public class ImportItemResult
    {
        public bool Success { get; set; }
        public bool IsCreation { get; set; }
        public ID ItemId { get; set; }
        public string ItemName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
