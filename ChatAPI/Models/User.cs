using System.ComponentModel.DataAnnotations;

namespace ChatAPI.Models
{
    public class User
    {
        [Key]
        public string Gid { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
        
        public List<string> messagedms { get; set; } = new List<string>();
    }
} 