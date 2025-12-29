using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Request
{
    public class MediaPostRequest
    {
        public Guid PostId { get; set; }
        public string MediaType { get; set; }
        public string MediaUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
