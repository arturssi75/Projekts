// Failā: Models/DeviceHistory.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models
{
    /// <summary>
    /// Reprezentē vienu ierakstu par ierīces atrašanās vietu noteiktā laika brīdī.
    /// </summary>
    public class DeviceHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int DeviceId { get; set; } // Ārējā atslēga uz Device

        [ForeignKey("DeviceId")]
        public virtual Device? Device { get; set; } // Navigācijas īpašība

        [Required]
        [Column(TypeName = "decimal(9,6)")] // Atbilstoši Device modelim
        public decimal Latitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(9,6)")] // Atbilstoši Device modelim
        public decimal Longitude { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Laiks, kad šis punkts tika fiksēts
    }
}
