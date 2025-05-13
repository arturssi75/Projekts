using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO transportlīdzekļa datu attēlošanai
    public class VehicleViewModel
    {
        public int VehicleId { get; set; }
        public string? LicensePlate { get; set; }
        public string? DriverName { get; set; }
        // Var pievienot info par piesaistīto kravu ID, ja tāda attiecība ir
        // public int? CurrentCargoId { get; set; }
    }

    // DTO jauna transportlīdzekļa izveidei
    public class VehicleCreateDto
    {
        [Required(ErrorMessage = "Valsts numura zīme ir obligāta.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Numura zīmei jābūt no 3 līdz 20 rakstzīmēm.")]
        public string? LicensePlate { get; set; }

        [Required(ErrorMessage = "Vadītāja vārds ir obligāts.")]
        [StringLength(100)]
        public string? DriverName { get; set; }
    }

    // DTO esoša transportlīdzekļa atjaunināšanai
    public class VehicleUpdateDto
    {
        [Required]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Valsts numura zīme ir obligāta.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Numura zīmei jābūt no 3 līdz 20 rakstzīmēm.")]
        public string? LicensePlate { get; set; }

        [Required(ErrorMessage = "Vadītāja vārds ir obligāts.")]
        [StringLength(100)]
        public string? DriverName { get; set; }
    }
}