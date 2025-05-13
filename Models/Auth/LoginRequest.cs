using System.ComponentModel.DataAnnotations;

namespace Project.Models.Auth
{
    /// <summary>
    /// Datu pārraides objekts (DTO) lietotāja pieteikšanās pieprasījumam.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Lietotājvārds vai e-pasts. Obligāts lauks.
        /// </summary>
        [Required(ErrorMessage = "Lietotājvārds vai e-pasts ir obligāts.")]
        public string? UserNameOrEmail { get; set; }

        /// <summary>
        /// Lietotāja parole. Obligāts lauks.
        /// </summary>
        [Required(ErrorMessage = "Parole ir obligāta.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}