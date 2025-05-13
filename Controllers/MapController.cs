using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Project.Models; 
using Project.Models.DTOs; 
using Project.Services;
using System.Security.Claims; 
using Microsoft.Extensions.Logging; // Pievienots ILogger
using System.Collections.Generic; // Pievienots IEnumerable
using System.Linq; // Pievienots Linq
using System.Threading.Tasks; // Pievienots Task

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class MapController : ControllerBase
    {
        private readonly MapService _mapService;
        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly CargoService _cargoService; 
        private readonly ILogger<MapController> _logger;

        public MapController(
            MapService mapService,
            UserManager<ApplicationUser> userManager,
            CargoService cargoService, 
            ILogger<MapController> logger)
        {
            _mapService = mapService;
            _userManager = userManager;
            _cargoService = cargoService;
            _logger = logger;
        }

        // GET: api/Map/GetDevices
        // Admin un Dispatcher redz visas ierīces ar koordinātēm.
        // Client redz tikai tās ierīces, kas piesaistītas viņa aktīvajām kravām.
        [HttpGet("GetDevices")] // Šis ir URL, ko izmanto MapManager
        [Authorize(Roles = "Admin,Dispatcher,Client")] // Pārliecināmies, ka Klients ir šeit
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MapDeviceViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDevicesForMap()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[MapController] GetDevicesForMap izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                IEnumerable<MapDeviceViewModel> devicesToReturn;

                if (User.IsInRole("Admin") || User.IsInRole("Dispatcher"))
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[MapController] Lietotājs ir Admin vai Dispatcher, ielādē visas aktīvās ierīces.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    devicesToReturn = await _mapService.GetActiveDevicesWithCoordinatesAsync();
                }
                else if (User.IsInRole("Client"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[MapController] GetDevicesForMap: Nevarēja iegūt UserId klientam.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Lietotāja ID nav atrasts tokenā."); // Atgriežam Forbid, ja ID nav atrasts
                    }
                    
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[MapController] Lietotājs ir Klients (UserId: {UserId}). Mēģina iegūt ApplicationUser.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    var appUser = await _userManager.FindByIdAsync(userId);
                    
                    if (appUser == null)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[MapController] GetDevicesForMap: ApplicationUser ar ID {UserId} nav atrasts.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return NotFound($"Lietotājs ar ID {userId} nav atrasts.");
                    }

                    if (!appUser.ClientId.HasValue)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                         _logger.LogWarning("[MapController] GetDevicesForMap: Klientam (AppUserId: {AppUserId}) nav piesaistīts ClientId.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Ok(new List<MapDeviceViewModel>()); // Atgriež tukšu sarakstu, ja nav ClientId
                    }
                    
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[MapController] Klientam (AppUserId: {UserId}, ClientId: {ClientId}) ielādē piesaistītās kravas.", userId, appUser.ClientId.Value);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    var clientCargos = await _cargoService.GetCargosByClientIdAsync(appUser.ClientId.Value);
                    
                    var clientDeviceIds = clientCargos
                        .SelectMany(c => c.Devices?.Select(d => d.DeviceId) ?? Enumerable.Empty<int>())
                        .Distinct()
                        .ToList();

                    if (!clientDeviceIds.Any())
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogInformation("[MapController] GetDevicesForMap: Klientam (ClientId: {ClientId}) nav ierīču piesaistītām kravām.", appUser.ClientId.Value);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Ok(new List<MapDeviceViewModel>());
                    }
                    
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[MapController] Klientam (ClientId: {ClientId}) atrastas šādas ierīču ID: {DeviceIds}. Ielādē to datus.", appUser.ClientId.Value, string.Join(",", clientDeviceIds));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    
                    // Iegūstam konkrēto ierīču datus kartei
                    // Šeit varētu būt optimizētāka metode MapService, kas pieņem List<int> deviceIds
                    var allActiveDevices = await _mapService.GetActiveDevicesWithCoordinatesAsync();
                    devicesToReturn = allActiveDevices.Where(d => clientDeviceIds.Contains(d.DeviceId)).ToList();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                     _logger.LogInformation("[MapController] Klientam (ClientId: {ClientId}) tiks atgrieztas {DeviceCount} ierīces.", appUser.ClientId.Value, devicesToReturn.Count());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[MapController] GetDevicesForMap: Lietotājam nav atbilstošu tiesību vai loma nav apstrādāta.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return Forbid(); 
                }
                
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[MapController] GetDevicesForMap atgrieza {Count} ierīces.", devicesToReturn.Count());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(devicesToReturn);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[MapController] Kļūda GetDevicesForMap metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot ierīces kartei.");
            }
        }
    }
}
