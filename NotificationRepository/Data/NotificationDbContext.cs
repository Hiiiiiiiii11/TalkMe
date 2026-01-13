using NotificationRepository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace NotificationRepository.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options) { }
        public DbSet<Notification> Notifications { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseLazyLoadingProxies();
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);
        }
    }
}
