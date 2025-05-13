using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models
{
    public enum DeviceType
    {
        GPS,
        RFID, // Pievienots piemēram
        Sensor // Pievienots piemēram
    }

    public class Device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int DeviceId { get; set; }

        public required DeviceType Type { get; set; } 

        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        public DateTime? LastUpdate { get; set; }
            
        public int? CargoId { get; set; } 
        [ForeignKey("CargoId")]
        public virtual Cargo? Cargo { get; set; }

        // Optimistic Concurrency kontrolei
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
