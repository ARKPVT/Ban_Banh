using Ban_Banh.Models;
using Microsoft.EntityFrameworkCore;

namespace Ban_Banh.Data;

public class FeedbackDbContext : DbContext
{
    public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : base(options) { }

    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackImage> FeedbackImages => Set<FeedbackImage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Feedback>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).IsRequired();
            e.HasMany(x => x.Images)
                .WithOne(x => x.Feedback!)
                .HasForeignKey(x => x.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CreatedAt);
        });

        b.Entity<FeedbackImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FeedbackId });
        });
    }
}
