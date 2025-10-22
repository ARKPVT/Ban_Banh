using Ban_Banh.Models;

namespace Ban_Banh.Models;

public class Feedback
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Rating { get; set; } = 5;
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<FeedbackImage> Images { get; set; } = new();
}

public class FeedbackImage
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = ""; // /uploads/feedback/{id}/{file}
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "";

    public Feedback? Feedback { get; set; }
}
