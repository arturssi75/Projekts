using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; 

namespace Project.Models
{
    public enum CargoStatus
    {
        Pending,
        InTransit,
        Delivered,
        Cancelled,
        RouteAssigned
    }
    public class Cargo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CargoId { get; set; }

        [ForeignKey("Sender")]
        [Required] 
        public int SenderId { get; set; }
        [ValidateNever] 
        public virtual Dispatcher? Sender { get; set; } 

        [ForeignKey("Receiver")]
        [Required] 
        public int ClientId { get; set; }
        [ValidateNever] 
        public virtual Client? Receiver { get; set; } 

        [Required] 
        public CargoStatus Status { get; set; } = CargoStatus.Pending;

        [ForeignKey("Route")]
        [Required] 
        public int RouteId { get; set; }
        [ValidateNever] 
        public virtual Route? Route { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("Vehicle")]
        public int? VehicleId { get; set; } 
        public virtual Vehicle? Vehicle { get; set; }

        [ValidateNever]
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>(); 
    
        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
