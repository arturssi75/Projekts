using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models
{
    public class Route // Saglabājam nosaukumu Route, jo DbContext jau izmanto alias DbRoute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RouteId { get; set; }
        
        public required string StartPoint { get; set; }
        
        public required string EndPoint { get; set; }
        
        public List<string> WayPoints { get; set; } = new();
        
        public DateTime EstimatedTime { get; set; }
        public virtual ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();

        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
