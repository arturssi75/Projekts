using System.ComponentModel.DataAnnotations;

namespace Project.Models.Auth
{
    // DTO reģistrācijas pieprasījumam
    public class RegisterRequest
    {
        [Required] // Lietotājvārds ir obligāts
        public required string Username { get; set; }

        [Required]
        [EmailAddress] // Pārbauda, vai formāts ir e-pasts
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)] // Norāda, ka šis ir paroles lauks
        public required string Password { get; set; }
        public required string Role { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        // Pievienojiet papildu laukus, ja nepieciešams reģistrācijai (piemēram, Vārds, Uzvārds)
        // public string? FirstName { get; set; }
        // public string? LastName { get; set; }

        // Lai atšķirtu klienta lomas (sūtītājs/saņēmējs), varat pievienot īpašības šeit
        // vai apstrādāt to atsevišķi pēc lietotāja izveides.
        // Piemērs:
        // public bool IsSender { get; set; } = false;
        // public bool IsReceiver { get; set; } = false;
    }
}