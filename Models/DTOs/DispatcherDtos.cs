using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO dispečera (sūtītāja) datu attēlošanai
    public class DispatcherViewModel
    {
        public int SenderId { get; set; } // Atbilstoši Dispatcher modelim
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        // public string? AssociatedUserEmail { get; set; } // Ja nepieciešams
    }

    // DTO jauna dispečera izveidei (parasti veido admins vai lietotājs reģistrējas ar šo lomu)
    public class DispatcherCreateDto
    {
        [Required(ErrorMessage = "Dispečera nosaukums ir obligāts.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nosaukumam jābūt no 2 līdz 100 rakstzīmēm.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Lūdzu, ievadiet derīgu e-pasta adresi.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Lūdzu, ievadiet derīgu tālruņa numuru.")]
        [StringLength(20)]
        public string? Phone { get; set; }
    }

    // DTO esoša dispečera atjaunināšanai
    public class DispatcherUpdateDto
    {
        [Required]
        public int SenderId { get; set; } // Atbilstoši Dispatcher modelim

        [Required(ErrorMessage = "Dispečera nosaukums ir obligāts.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nosaukumam jābūt no 2 līdz 100 rakstzīmēm.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Lūdzu, ievadiet derīgu e-pasta adresi.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Lūdzu, ievadiet derīgu tālruņa numuru.")]
        [StringLength(20)]
        public string? Phone { get; set; }
    }
}