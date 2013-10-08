using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCrawlerRole.Model
{
    public class SnapshotConfig
    {
        public string ApiId { get; set; }
        public string Application { get; set; }
        public string Url { get; set; }
        public bool Store { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string UserAgent { get; set; }
    }
}
