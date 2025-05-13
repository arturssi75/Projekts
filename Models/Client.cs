using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Project.Models
{
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClientId { get; set; }

        public required string Name { get; set; }

        public virtual ICollection<Cargo> ReceivedCargos { get; set; } = new List<Cargo>();

        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
