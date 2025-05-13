using Project.Models; // Nepieciešams CargoStatus un citiem enum/modeļiem
using System.ComponentModel.DataAnnotations;

namespace Project.Models.DTOs
{
    // DTO kravas datu attēlošanai (piemēram, sarakstos vai detaļu skatā)
    public class CargoViewModel
    {
        public int CargoId { get; set; }
        public CargoStatus Status { get; set; } // Enum tiks konvertēts uz string, pateicoties Program.cs konfigurācijai

        public int SenderId { get; set; }
        public string? SenderName { get; set; } // Sūtītāja vārds

        public int ClientId { get; set; }
        public string? ClientName { get; set; } // Klienta (saņēmēja) vārds

        public int RouteId { get; set; }
        public string? RouteDescription { get; set; } // Maršruta apraksts (piem., "Rīga - Liepāja")

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Piesaistītās ierīces (varētu būt vienkāršots skats)
        public List<DeviceSimpleViewModel> Devices { get; set; } = new List<DeviceSimpleViewModel>();
    }

    // DTO jaunas kravas izveidei
    public class CargoCreateDto
    {
        [Required(ErrorMessage = "Kravas statuss ir obligāts.")]
        public CargoStatus Status { get; set; }

        [Required(ErrorMessage = "Sūtītāja ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sūtītāja ID jābūt derīgam skaitlim.")]
        public int SenderId { get; set; } // Attiecas uz Dispatcher.SenderId

        [Required(ErrorMessage = "Klienta (saņēmēja) ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Klienta ID jābūt derīgam skaitlim.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Maršruta ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Maršruta ID jābūt derīgam skaitlim.")]
        public int RouteId { get; set; }

        // Saraksts ar ierīču ID, kuras piesaistīt kravai.
        // Varētu būt arī sarežģītāks objekts, ja nepieciešama papildu info par piesaisti.
        public List<int>? DeviceIds { get; set; } = new List<int>();
    }

    // DTO esošas kravas atjaunināšanai
    public class CargoUpdateDto
    {
        [Required]
        public int CargoId { get; set; } // ID ir obligāts, lai identificētu kravu

        [Required(ErrorMessage = "Kravas statuss ir obligāts.")]
        public CargoStatus Status { get; set; }

        [Required(ErrorMessage = "Sūtītāja ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sūtītāja ID jābūt derīgam skaitlim.")]
        public int SenderId { get; set; }

        [Required(ErrorMessage = "Klienta (saņēmēja) ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Klienta ID jābūt derīgam skaitlim.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Maršruta ID ir obligāts.")]
        [Range(1, int.MaxValue, ErrorMessage = "Maršruta ID jābūt derīgam skaitlim.")]
        public int RouteId { get; set; }

        // Saraksts ar ierīču ID, kuras būs piesaistītas kravai pēc atjaunināšanas.
        // Servisā būs jāapstrādā esošo ierīču atsaistīšana un jauno piesaistīšana.
        public List<int>? DeviceIds { get; set; } = new List<int>();
    }

    // Vienkāršots DTO ierīcei, ko izmantot CargoViewModel
    public class DeviceSimpleViewModel
    {
        public int DeviceId { get; set; }
        public DeviceType Type { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime? LastUpdate { get; set; }
    }
}