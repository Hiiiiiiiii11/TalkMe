using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model
{
    public class PostMedias
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PostId { get; set; }
        public int MediaType { get; set; }
        public string MediaUrl { get; set; }
        public int SortOrder { get; set; }
        public Posts Post { get; set; }
    }

}
