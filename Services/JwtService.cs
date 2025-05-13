using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration; // Nepieciešams IConfiguration
using Microsoft.IdentityModel.Tokens;
using Project.Models; // Nepieciešams ApplicationUser
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt; // Nepieciešams JwtSecurityTokenHandler, JwtRegisteredClaimNames
using System.Security.Claims; // Nepieciešams Claim, ClaimTypes
using System.Text; // Nepieciešams Encoding
using System.Threading.Tasks;

namespace Project.Services
{
    /// <summary>
    /// Serviss JWT (JSON Web Token) ģenerēšanai un validācijai (ja nepieciešams).
    /// </summary>
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager; // Izmantojam ApplicationUser, lai piekļūtu papildu īpašībām, ja nepieciešams

        /// <summary>
        /// JwtService konstruktors.
        /// </summary>
        /// <param name="configuration">Lietojumprogrammas konfigurācija (piekļuvei Jwt:Key, Jwt:Issuer, Jwt:Audience no appsettings.json).</param>
        /// <param name="userManager">ASP.NET Core Identity lietotāju pārvaldnieks (lai iegūtu lietotāja lomas un claimus).</param>
        public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Ģenerē JWT tokenu norādītajam lietotājam.
        /// </summary>
        /// <param name="user">Lietotājs (ApplicationUser objekts), kuram ģenerēt tokenu.</param>
        /// <returns>Ģenerētais JWT tokens kā string.</returns>
        public async Task<string> GenerateToken(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrEmpty(user.UserName)) throw new ArgumentException("User name cannot be null or empty.", nameof(user.UserName));
            if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User ID cannot be null or empty.", nameof(user.Id));


            // Iegūstam lietotāja lomas no UserManager
            var roles = await _userManager.GetRolesAsync(user);
            // Iegūstam lietotāja pielāgotos claimus (ja tādi ir definēti)
            var userClaims = await _userManager.GetClaimsAsync(user);

            // Izveidojam sarakstu ar claimiem, kas tiks iekļauti tokenā
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id), // Lietotāja unikālais ID (subjekts)
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Standarta subjekta claims
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unikāls tokena identifikators
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Tokena izdošanas laiks
            };

            // Pievienojam lietotājvārdu kā claimu (ja tas nav null vai tukšs)
            if (!string.IsNullOrEmpty(user.UserName))
            {
                authClaims.Add(new Claim(ClaimTypes.Name, user.UserName));
                authClaims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)); // Alternatīva lietotājvārdam
            }

            // Pievienojam e-pastu kā claimu (ja tas nav null vai tukšs)
            if (!string.IsNullOrEmpty(user.Email))
            {
                authClaims.Add(new Claim(ClaimTypes.Email, user.Email));
                authClaims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            }

            // Pievienojam lietotāja lomas kā atsevišķus ClaimTypes.Role claimus
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Pievienojam visus pārējos lietotāja specifiskos claimus
            authClaims.AddRange(userClaims);

            // Iegūstam JWT konfigurācijas vērtības no appsettings.json
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.json.");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured in appsettings.json.");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured in appsettings.json.");

            // Izveidojam simetrisko drošības atslēgu no konfigurācijas atslēgas
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            // Definējam tokena derīguma termiņu (piemēram, 1 stunda no šī brīža)
            var tokenExpiry = DateTime.UtcNow.AddHours(1); // Pielāgo pēc nepieciešamības

            // Izveidojam JWT tokenu ar norādītajiem parametriem
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(authClaims), // Claimi, kas identificē lietotāju
                Expires = tokenExpiry,                    // Tokena derīguma termiņš
                Issuer = jwtIssuer,                       // Tokena izdevējs
                Audience = jwtAudience,                   // Tokena auditorija/saņēmējs
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256Signature) // Parakstīšanas algoritms un atslēga
            };

            // Izveidojam tokena apstrādātāju un serializējam tokenu string formātā
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token); // Atgriežam ģenerēto tokenu
        }
    }
}