using Project.Models; 
using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO ierīces datu attēlošanai
    public class DeviceViewModel
    {
        public int DeviceId { get; set; }
        public DeviceType Type { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int? CargoId { get; set; } 
    }

    // DTO jaunas ierīces izveidei
    public class DeviceCreateDto
    {
        [Required(ErrorMessage = "Ierīces tips ir obligāts.")]
        public DeviceType Type { get; set; }

        [Required(ErrorMessage = "Platums (Latitude) ir obligāts.")]
        [Range(-90.0, 90.0, ErrorMessage = "Platums jābūt robežās no -90 līdz 90.")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Garums (Longitude) ir obligāts.")]
        [Range(-180.0, 180.0, ErrorMessage = "Garums jābūt robežās no -180 līdz 180.")]
        public decimal Longitude { get; set; }
    }

    // DTO esošas ierīces atjaunināšanai
    public class DeviceUpdateDto
    {
        [Required]
        public int DeviceId { get; set; }

        [Required(ErrorMessage = "Ierīces tips ir obligāts.")]
        public DeviceType Type { get; set; }

        [Required(ErrorMessage = "Platums (Latitude) ir obligāts.")]
        [Range(-90.0, 90.0, ErrorMessage = "Platums jābūt robežās no -90 līdz 90.")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Garums (Longitude) ir obligāts.")]
        [Range(-180.0, 180.0, ErrorMessage = "Garums jābūt robežās no -180 līdz 180.")]
        public decimal Longitude { get; set; }

        public int? CargoId { get; set; }
    }

    // DTO ierīču datu attēlošanai kartē (no MapController)
    public class MapDeviceViewModel
    {
        public int DeviceId { get; set; }
        public required string Type { get; set; } 
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int? CargoId { get; set; }
    }

    // JAUNS: DTO ierīces vēstures punktam
    public class DeviceHistoryPointDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
