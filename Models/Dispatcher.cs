using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; 

namespace Project.Models
{
    public class Dispatcher
    {
        [Key]
        public int SenderId { get; set; } 

        public required string Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public virtual ICollection<Cargo> SentCargos { get; set; } = new List<Cargo>();

        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
