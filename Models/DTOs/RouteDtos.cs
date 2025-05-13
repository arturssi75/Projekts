using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO maršruta datu attēlošanai
    public class RouteViewModel
    {
        public int RouteId { get; set; }
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public List<string> WayPoints { get; set; } = new List<string>();
        public DateTime EstimatedTime { get; set; }
        // Var pievienot informāciju par piesaistītajām kravām, ja nepieciešams
        // public int ActiveCargosCount { get; set; }
    }

    // DTO jauna maršruta izveidei
    public class RouteCreateDto
    {
        [Required(ErrorMessage = "Sākuma punkts ir obligāts.")]
        [StringLength(100)]
        public string? StartPoint { get; set; }

        [Required(ErrorMessage = "Beigu punkts ir obligāts.")]
        [StringLength(100)]
        public string? EndPoint { get; set; }

        public List<string>? WayPoints { get; set; } = new List<string>();

        [Required(ErrorMessage = "Paredzamais laiks ir obligāts.")]
        public DateTime EstimatedTime { get; set; }
    }

    // DTO esoša maršruta atjaunināšanai
    public class RouteUpdateDto
    {
        [Required]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Sākuma punkts ir obligāts.")]
        [StringLength(100)]
        public string? StartPoint { get; set; }

        [Required(ErrorMessage = "Beigu punkts ir obligāts.")]
        [StringLength(100)]
        public string? EndPoint { get; set; }

        public List<string>? WayPoints { get; set; } = new List<string>();

        [Required(ErrorMessage = "Paredzamais laiks ir obligāts.")]
        public DateTime EstimatedTime { get; set; }
    }
}