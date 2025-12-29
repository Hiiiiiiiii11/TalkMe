using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model
{
    public class Posts
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Content { get; set; }
        public int PrivacyLevel { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<Comments> Comments { get; set; }
        public ICollection<Likes> Likes { get; set; }
        public virtual ICollection<PostMedias> PostMedias { get; set; } = new List<PostMedias>();
    }

}