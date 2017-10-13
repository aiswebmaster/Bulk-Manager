using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Feature.BulkManager.Core.Models
{
    public class ImportResult
    {
        public ImportResult()
        {
            ItemResults = new List<ImportItemResult>();
        }

        public int ItemsUpdated { get; set; }
        public int ItemsCreated { get; set; }
        public int ItemsFailed { get; set; }
        public IList<ImportItemResult> ItemResults { get; set; }
    }
}
