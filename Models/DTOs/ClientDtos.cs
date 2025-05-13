using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO klienta datu attēlošanai
    public class ClientViewModel
    {
        public int ClientId { get; set; }
        public string? Name { get; set; }
        // Šeit var pievienot citu informāciju, piemēram, saistītā ApplicationUser e-pastu, ja nepieciešams
        // public string? AssociatedUserEmail { get; set; }
    }

    // DTO jauna klienta izveidei
    public class ClientCreateDto
    {
        [Required(ErrorMessage = "Klienta nosaukums ir obligāts.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Klienta nosaukumam jābūt no 2 līdz 100 rakstzīmēm.")]
        public string? Name { get; set; }

        // Ja klientu veido admins un uzreiz piesaista jaunam ApplicationUser (sarežģītāks scenārijs)
        // public string? Email { get; set; } // E-pasts jaunajam ApplicationUser
        // public string? Password { get; set; } // Parole jaunajam ApplicationUser
    }

    // DTO esoša klienta atjaunināšanai
    public class ClientUpdateDto
    {
        [Required]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Klienta nosaukums ir obligāts.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Klienta nosaukumam jābūt no 2 līdz 100 rakstzīmēm.")]
        public string? Name { get; set; }
    }
}