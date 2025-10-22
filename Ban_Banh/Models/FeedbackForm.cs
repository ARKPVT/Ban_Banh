using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Ban_Banh.Models
{
    public class FeedbackForm
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int? Rating { get; set; }
        public string? Message { get; set; }
        public IList<IFormFile>? Files { get; set; }
    }
}