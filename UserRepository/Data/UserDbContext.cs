using Microsoft.EntityFrameworkCore;
using UserService.Model;
using UserRepository.Models;
using UserRepository.Model;


namespace UserRepository.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

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
            base.OnModelCreating(modelBuilder);

            // User table
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
            });

            // PasswordResetToken table
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Token).IsRequired();
                entity.HasOne(t => t.User)
                      .WithMany(u => u.PasswordResetTokens)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // EmailVerification table
            modelBuilder.Entity<EmailVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).IsRequired();
            });
        }
    }
}
// lenh migration
// dotnet ef migrations add addAvartarGroupfix --project ChatRepository --startup-project UserChatAPI --context ChatDbContext
// dotnet ef database update --project ChatRepository --startup-project UserChatAPI --context ChatDbContext
