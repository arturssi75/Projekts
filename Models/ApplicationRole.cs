using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    // ApplicationRole klase paplašina IdentityRole.
    // Jūs varat pievienot papildu īpašības savām lomām šeit, ja nepieciešams.
    public class ApplicationRole : IdentityRole
    {
        [StringLength(256)]
        public string? Description { get; set; }
        public ApplicationRole() : base()
        {
        }
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
