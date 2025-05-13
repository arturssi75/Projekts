using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models
{
    public class Vehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VehicleId { get; set; }
        
        public string? LicensePlate { get; set; }
        
        public string? DriverName { get; set; }
        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}