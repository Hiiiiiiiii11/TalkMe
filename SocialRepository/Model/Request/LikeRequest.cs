using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Request
{
    public class LikeRequest
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
    }
}
