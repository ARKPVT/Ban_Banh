using Microsoft.EntityFrameworkCore;
using Ban_Banh.Models;

namespace Ban_Banh.Data
{
    public class FeedbackDbContext : DbContext
    {
        public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : base(options) { }

        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<FeedbackImage> FeedbackImage { get; set; }
    }
}
