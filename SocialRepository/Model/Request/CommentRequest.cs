using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Request
{
    public class CommentRequest
    {

        public string Content { get; set; }
        public Guid? ParentCommentId { get; set; }
    }

    public class CommentUpdateRequest
    {
        public string Content { get; set; }
        public Guid? ParentCommentId { get; set; }
    }
}
