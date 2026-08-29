using System.ComponentModel.DataAnnotations;

namespace ChatAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        public string Content { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public string SenderId { get; set; } = string.Empty;
        
        public string ReceiverId { get; set; } = string.Empty;
        
        public bool Seen { get; set; } = false;
    }
} 