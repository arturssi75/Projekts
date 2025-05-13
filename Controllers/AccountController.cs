using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Models.Auth; // Pārliecinieties, ka LoginRequest un RegisterRequest ir šeit
using Project.Models.DTOs; // Pievienojam DTO vārdtelpu
using Project.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager; // Pievienots, ja nepieciešams tieši strādāt ar lomām šeit
        private readonly JwtService _jwtService;
        private readonly TransportContext _context; // Nepieciešams Client/Dispatcher izveidei
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager, // Pievienots
            JwtService jwtService,
            TransportContext context, // Pievienots
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager; // Pievienots
            _jwtService = jwtService;
            _context = context; // Pievienots
            _logger = logger;
        }

        // POST: api/Account/Register
        [HttpPost("Register")]
        [AllowAnonymous] // Atļauj anonīmu piekļuvi reģistrācijai
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))] // Atgriežam DTO
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("Mēģinājums reģistrēt lietotāju: {Username}", request.Username);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("Reģistrācijas pieprasījums nav derīgs: {Username}", request.Username);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }

            // Pārbaude, vai loma ir atļauta reģistrācijai
            if (request.Role != "Client" && request.Role != "Dispatcher")
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("Nederīga loma '{Role}' reģistrācijai: {Username}", request.Role, request.Username);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { Message = $"Loma '{request.Role}' nav atļauta reģistrācijai. Atļautās lomas: Client, Dispatcher." });
            }

            // Pārbaude, vai loma eksistē sistēmā
            if (!await _roleManager.RoleExistsAsync(request.Role).ConfigureAwait(false))
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogError("Loma '{Role}' neeksistē sistēmā. Lietotājs: {Username}", request.Role, request.Username);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Sistēmas kļūda: Loma '{request.Role}' nav definēta." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Username, // Lietotājvārds
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true // Automātiski apstiprinām e-pastu
            };

            var createUserResult = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
            if (!createUserResult.Succeeded)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("Neizdevās izveidot lietotāju {Email}: {Errors}", request.Email, string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { Message = "Neizdevās reģistrēt lietotāju.", Errors = createUserResult.Errors.Select(e => e.Description) });
            }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("Lietotājs {Email} veiksmīgi izveidots.", request.Email);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

            var addToRoleResult = await _userManager.AddToRoleAsync(user, request.Role).ConfigureAwait(false);
            if (!addToRoleResult.Succeeded)
            {
                // Ja neizdodas piešķirt lomu, dzēšam tikko izveidoto lietotāju, lai sistēma paliktu konsekventa
                await _userManager.DeleteAsync(user).ConfigureAwait(false);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError("Neizdevās piešķirt lomu '{Role}' lietotājam {Email}. Lietotājs dzēsts. Kļūdas: {Errors}", request.Role, request.Email, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Lietotājs tika izveidots, bet neizdevās piešķirt lomu. Reģistrācija atcelta.", Errors = addToRoleResult.Errors.Select(e => e.Description) });
            }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
             _logger.LogInformation("Loma '{Role}' piešķirta lietotājam '{Email}'.", request.Role, request.Email);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

            // Izveidojam saistīto Client vai Dispatcher entītiju
            try
            {
                if (request.Role == "Client")
                {
                    var newClient = new Client { Name = $"{user.FirstName} {user.LastName}".Trim() == "" ? user.UserName : $"{user.FirstName} {user.LastName}".Trim() };
                    _context.Client.Add(newClient);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    user.ClientId = newClient.ClientId; // Piešķiram int? vērtību
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("Client entītija izveidota ar ID {ClientId} lietotājam '{Email}'.", newClient.ClientId, user.Email);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else if (request.Role == "Dispatcher")
                {
                    var newDispatcher = new Dispatcher { Name = $"{user.FirstName} {user.LastName}".Trim() == "" ? user.UserName : $"{user.FirstName} {user.LastName}".Trim(), Email = user.Email };
                    _context.Dispatcher.Add(newDispatcher);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    user.DispatcherId = newDispatcher.SenderId; // Piešķiram int? vērtību
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("Dispatcher entītija izveidota ar ID {DispatcherId} lietotājam '{Email}'.", newDispatcher.SenderId, user.Email);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                var updateUserResult = await _userManager.UpdateAsync(user).ConfigureAwait(false); // Saglabājam ClientId/DispatcherId lietotājam
                if (!updateUserResult.Succeeded)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogError("Neizdevās atjaunināt lietotāju '{Email}' ar ClientId/DispatcherId. Kļūdas: {Errors}", user.Email, string.Join(", ", updateUserResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    // Apsveriet, vai šeit vajadzētu atcelt Client/Dispatcher izveidi, ja lietotāja atjaunināšana neizdodas
                }
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "Kļūda veidojot saistīto Client/Dispatcher entītiju lietotājam '{Email}'.", user.Email);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                // Apsveriet lietotāja dzēšanu, ja saistītās entītijas izveide neizdodas, lai uzturētu datu integritāti
                await _userManager.DeleteAsync(user).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Kļūda izveidojot saistīto klienta/dispečera profilu. Reģistrācija atcelta." });
            }

            var token = await _jwtService.GenerateToken(user).ConfigureAwait(false);
            var userRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("Lietotājs '{Email}' veiksmīgi reģistrēts un tokens ģenerēts. Lomas: {UserRoles}", user.Email, string.Join(",", userRoles));
#pragma warning restore CA1848 // Use the LoggerMessage delegates

            return Ok(new LoginResponseDto // Izmantojam DTO
            {
                Message = "Reģistrācija veiksmīga.",
                Token = token,
                UserId = user.Id,
                Username = user.UserName ?? "N/A", // Nodrošinām, ka UserName nav null
                Email = user.Email ?? "N/A",    // Nodrošinām, ka Email nav null
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles,
                ClientId = user.ClientId,
                DispatcherId = user.DispatcherId
            });
        }

        // POST: api/Account/Login
        [HttpPost("Login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))] // Atgriežam DTO
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("Mēģinājums pieteikties: {UserNameOrEmail}", request.UserNameOrEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("Pieteikšanās pieprasījums nav derīgs: {UserNameOrEmail}", request.UserNameOrEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail ?? string.Empty).ConfigureAwait(false);
            if (user == null && request.UserNameOrEmail != null && request.UserNameOrEmail.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(request.UserNameOrEmail).ConfigureAwait(false);
            }

            if (user == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogWarning("Pieteikšanās neizdevās: Lietotājs '{UserNameOrEmail}' nav atrasts.", request.UserNameOrEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Unauthorized(new { Message = "Nederīgs lietotājvārds/e-pasts vai parole." });
            }

            var passwordSignInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? string.Empty, lockoutOnFailure: false).ConfigureAwait(false);

            if (passwordSignInResult.Succeeded)
            {
                var userRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
                var token = await _jwtService.GenerateToken(user).ConfigureAwait(false);

#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("Lietotājs '{UserNameOrEmail}' veiksmīgi pieteicies. Lomas: {UserRoles}", request.UserNameOrEmail, string.Join(",", userRoles));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(new LoginResponseDto // Izmantojam DTO
                {
                    Message = "Pieteikšanās veiksmīga.",
                    Token = token,
                    UserId = user.Id,
                    Username = user.UserName ?? "N/A",
                    Email = user.Email ?? "N/A",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = userRoles,
                    ClientId = user.ClientId,
                    DispatcherId = user.DispatcherId
               });
            }

#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogWarning("Pieteikšanās neizdevās lietotājam '{UserNameOrEmail}': Nederīgi akreditācijas dati.", request.UserNameOrEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            return Unauthorized(new { Message = "Nederīgs lietotājvārds/e-pasts vai parole." });
        }

        // GET: api/Account/CurrentUser
        [HttpGet("CurrentUser")]
        [Authorize] // Tikai autentificēti lietotāji var piekļūt
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))] // Atgriežam DTO
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("GetCurrentUser: UserId nav atrasts tokena pieprasījumos.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Unauthorized(new { Message = "Nevarēja identificēt lietotāju no tokena." });
            }

            var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("GetCurrentUser: Lietotājs ar ID '{UserId}' nav atrasts.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return NotFound(new { Message = "Lietotājs nav atrasts." });
            }

            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("Iegūta informācija par pašreizējo lietotāju ID '{UserId}'.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            return Ok(new LoginResponseDto // Izmantojam DTO
            {
                // Message nav nepieciešams šeit
                UserId = user.Id,
                Username = user.UserName ?? "N/A",
                Email = user.Email ?? "N/A",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                ClientId = user.ClientId,
                DispatcherId = user.DispatcherId
            });
        }
    }
}
