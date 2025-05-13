using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using Project.Models.DTOs; // Pieņemot, ka DTO ir šeit
using Project.Services;
using System.Security.Claims; // Nepieciešams ClaimTypes

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Visas metodes šajā kontrolierī pēc noklusējuma prasa autentifikāciju
    public class CargoController : ControllerBase
    {
        private readonly CargoService _cargoService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CargoController> _logger;

        public CargoController(
            CargoService cargoService,
            UserManager<ApplicationUser> userManager,
            ILogger<CargoController> logger)
        {
            _cargoService = cargoService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/Cargo
        // Pieejams Admin, Dispatcher un Client lomām.
        // Klientiem tiks atgrieztas tikai viņu kravas.
        [HttpGet]
        [Authorize(Roles = "Admin,Dispatcher,Client")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CargoViewModel>))] // Piemērs ar ViewModel
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCargos()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoController] GetAllCargos izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                IEnumerable<CargoViewModel> cargosToReturn;

                if (User.IsInRole("Client"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[CargoController] GetAllCargos: Nevarēja iegūt UserId klientam.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid();
                    }

                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.ClientId.HasValue)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[CargoController] GetAllCargos: Klients (AppUserId: {AppUserId}) nav atrasts vai tam nav piesaistīts ClientId.", userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Ok(new List<CargoViewModel>()); // Atgriež tukšu sarakstu
                    }
                    cargosToReturn = await _cargoService.GetCargosByClientIdAsync(appUser.ClientId.Value);
                }
                else // Admin vai Dispatcher
                {
                    cargosToReturn = await _cargoService.GetAllCargosAsync();
                }

#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoController] GetAllCargos atgrieza {Count} kravas.", cargosToReturn.Count());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(cargosToReturn);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoController] Kļūda GetAllCargos metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot kravas.");
            }
        }

        // GET: api/Cargo/{id}
        // Pieejams Admin, Dispatcher. Klientam pieejams, ja tā ir viņa krava.
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Dispatcher,Client")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CargoViewModel))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCargoById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoController] GetCargoById({CargoId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var cargo = await _cargoService.GetCargoByIdAsync(id);
                if (cargo == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[CargoController] GetCargoById({CargoId}): Krava nav atrasta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Krava ar ID {id} nav atrasta.");
                }

                if (User.IsInRole("Client"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId)) return Forbid();

                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.ClientId.HasValue || cargo.ClientId != appUser.ClientId.Value)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[CargoController] GetCargoById({CargoId}): Klients (AppUserId: {AppUserId}) mēģina piekļūt svešai kravai.", id, userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Jums nav tiesību piekļūt šai kravai.");
                    }
                }
                return Ok(cargo);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoController] Kļūda GetCargoById({CargoId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot kravu.");
            }
        }

        // POST: api/Cargo
        // Pieejams tikai Admin un Dispatcher lomām.
        [HttpPost]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CargoViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCargo([FromBody] CargoCreateDto cargoDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoController] CreateCargo izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoController] CreateCargo: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdCargo = await _cargoService.CreateCargoAsync(cargoDto);
                if (createdCargo == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogError("[CargoController] CreateCargo: Neizdevās izveidot kravu (serviss atgrieza null).");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return StatusCode(StatusCodes.Status500InternalServerError, "Neizdevās izveidot kravu. Iespējams, norādītie ID (SenderId, ClientId, RouteId) nav derīgi.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoController] CreateCargo: Krava ar ID {CargoId} veiksmīgi izveidota.", createdCargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                // Atgriežam ViewModel, nevis tieši entītiju
                return CreatedAtAction(nameof(GetCargoById), new { id = createdCargo.CargoId }, createdCargo);
            }
            catch (ArgumentException argEx) // Piemēram, ja serviss izmet par nederīgiem ID
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(argEx, "[CargoController] CreateCargo: Argumenta kļūda.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = argEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoController] Kļūda CreateCargo metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot kravu: {ex.Message}");
            }
        }

        // PUT: api/Cargo/{id}
        // Pieejams tikai Admin un Dispatcher lomām.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CargoViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCargo(int id, [FromBody] CargoUpdateDto cargoDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoController] UpdateCargo({CargoId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoController] UpdateCargo({CargoId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}", id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != cargoDto.CargoId) // Pārbaude, vai ID sakrīt
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoController] UpdateCargo({CargoId}): ID nesakrīt ar DTO ID ({DtoCargoId}).", id, cargoDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID nesakrīt ar objekta ID.");
            }

            try
            {
                var updatedCargo = await _cargoService.UpdateCargoAsync(cargoDto);
                if (updatedCargo == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[CargoController] UpdateCargo({CargoId}): Krava nav atrasta vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Krava ar ID {id} nav atrasta vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoController] UpdateCargo({CargoId}): Krava veiksmīgi atjaunināta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedCargo);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[CargoController] Konkurences kļūda UpdateCargo({CargoId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Krava, iespējams, tika modificēta vienlaicīgi. Lūdzu, atsvaidziniet datus un mēģiniet vēlreiz.");
            }
            catch (ArgumentException argEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(argEx, "[CargoController] UpdateCargo({CargoId}): Argumenta kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = argEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoController] Kļūda UpdateCargo({CargoId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot kravu: {ex.Message}");
            }
        }

        // DELETE: api/Cargo/{id}
        // Pieejams tikai Admin un Dispatcher lomām.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCargo(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoController] DeleteCargo({CargoId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _cargoService.DeleteCargoAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[CargoController] DeleteCargo({CargoId}): Krava nav atrasta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Krava ar ID {id} nav atrasta.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoController] DeleteCargo({CargoId}): Krava veiksmīgi dzēsta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Krava ar ID {id} veiksmīgi dzēsta.");
            }
            catch (InvalidOperationException opEx) // Piemēram, ja nevar dzēst piesaistītu kravu
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogWarning(opEx, "[CargoController] DeleteCargo({CargoId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoController] Kļūda DeleteCargo({CargoId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot kravu: {ex.Message}");
            }
        }

        // Palīgmetode, lai iegūtu ModelState kļūdas kā string
        private string ModelStateValuesErrorMessages()
        {
            return string.Join("; ", ModelState.Values
                                .SelectMany(x => x.Errors)
                                .Select(x => x.ErrorMessage));
        }
    }
}