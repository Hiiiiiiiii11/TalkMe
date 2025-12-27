using Microsoft.EntityFrameworkCore;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Data
{
    public class SocialDbContext : DbContext
    {
        public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }
        public DbSet<Likes> Likes { get; set; }
        public DbSet<Comments> Comments { get; set; }
        public DbSet<PostMedias> PostMedias { get; set; }
        public DbSet<Posts> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseLazyLoadingProxies();
            }
        }

    }
    //dotnet ef migrations add InitialCreate --project SocialRepository --startup-project SocialApi --context SocialDbContext
    // dotnet ef database update --project SocialRepository --startup-project SocialApi --context SocialDbContext
}
