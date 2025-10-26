using System.ComponentModel.DataAnnotations;

namespace Ban_Banh.Models;

public class Feedback
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string? Name { get; set; }

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required]
    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FeedbackImage> Images { get; set; } = new List<FeedbackImage>();
}

public class FeedbackImage
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public Feedback? Feedback { get; set; }

    [MaxLength(255)]
    public string FileName { get; set; } = "";

    [MaxLength(255)]
    public string ContentType { get; set; } = "";

    public long Size { get; set; }

    /// <summary>Relative URL to show image: /uploads/feedback/{feedbackId}/{fileName}</summary>
    [MaxLength(512)]
    public string Url { get; set; } = "";
}
