    using System.Collections.Generic; // Nepieciešams priekš IList

    namespace Project.Models.DTOs
    {
        public class LoginResponseDto
        {
            public string? Message { get; set; }
            public string? Token { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public IList<string> Roles { get; set; } = new List<string>();
            public int? ClientId { get; set; }
            public int? DispatcherId { get; set; }
        }
    }
    