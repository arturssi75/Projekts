using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Pievienojiet šo


namespace Project.Models
{
    // ApplicationUser klase paplašina IdentityUser un ļauj pievienot papildu īpašības
    // saviem lietotājiem, kas nav iekļautas noklusējuma IdentityUser klasē.
    public class ApplicationUser : IdentityUser
    {
        // Piemērs: Pievienojiet vārda un uzvārda īpašības
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }
        public int? ClientId { get; set; }
        public int? DispatcherId { get; set; }
        // ------------ NAVIGĀCIJAS ĪPAŠĪBAS ------------
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        [ForeignKey("DispatcherId")]
        public virtual Dispatcher? Dispatcher { get; set; }
        
    }
}
