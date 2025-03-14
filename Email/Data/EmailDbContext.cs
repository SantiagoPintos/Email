using Microsoft.EntityFrameworkCore;

namespace Email.Data
{
    public class EmailDbContext : DbContext
    {
        public DbSet<EmailMessage> Emails { get; set; }
        public DbSet<EmailAttachment> Attachments { get; set; }

        public EmailDbContext(DbContextOptions<EmailDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailMessage>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<EmailAttachment>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<EmailAttachment>()
                .HasOne<EmailMessage>()
                .WithMany(e => e.Attachments)
                .HasForeignKey(a => a.EmailMessageId);
        }
    }
}