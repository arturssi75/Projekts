using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Project.Models; // ApplicationUser
using Project.Models.DTOs; // ClientViewModel, ClientCreateDto, ClientUpdateDto
using Project.Services;
using System.Security.Claims; // Nepieciešams ClaimTypes

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Visas metodes prasa autentifikāciju
    public class ClientController : ControllerBase
    {
        private readonly ClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientController> _logger;

        public ClientController(
            ClientService clientService,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientController> logger)
        {
            _clientService = clientService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/Client
        // Admin un Dispatcher var redzēt visus klientus.
        [HttpGet]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ClientViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllClients()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientController] GetAllClients izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                return Ok(clients);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientController] Kļūda GetAllClients metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot klientus.");
            }
        }

        // GET: api/Client/{id}
        // Admin un Dispatcher var redzēt jebkuru klientu.
        // Klients (Client loma) var redzēt tikai savu profilu.
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Dispatcher,Client")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientViewModel))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetClientById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientController] GetClientById({ClientId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                if (User.IsInRole("Client"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId)) return Forbid("Lietotāja ID nav atrasts tokenā.");

                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.ClientId.HasValue || appUser.ClientId.Value != id)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[ClientController] GetClientById({ClientId}): Klients (AppUserId: {AppUserId}) mēģina piekļūt svešam profilam vai nav piesaistīts ClientId.", id, userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Jums nav tiesību piekļūt šī klienta datiem.");
                    }
                }
                // Admin un Dispatcher var piekļūt jebkuram

                var clientViewModel = await _clientService.GetClientByIdAsync(id);
                if (clientViewModel == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[ClientController] GetClientById({ClientId}): Klients nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Klients ar ID {id} nav atrasts.");
                }
                return Ok(clientViewModel);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientController] Kļūda GetClientById({ClientId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot klientu.");
            }
        }

        // POST: api/Client
        // Tikai Admin un Dispatcher var manuāli izveidot jaunus klientu profilus.
        // Paši klienti reģistrējas caur AccountController.
        [HttpPost]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ClientViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClient([FromBody] ClientCreateDto clientDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientController] CreateClient izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientController] CreateClient: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdClient = await _clientService.CreateClientAsync(clientDto);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientController] CreateClient: Klients ar ID {ClientId} veiksmīgi izveidots.", createdClient.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return CreatedAtAction(nameof(GetClientById), new { id = createdClient.ClientId }, createdClient);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientController] Kļūda CreateClient metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot klientu: {ex.Message}");
            }
        }

        // PUT: api/Client/{id}
        // Admin un Dispatcher var atjaunināt klientu datus.
        // Klients varētu atjaunināt savu profilu (nepieciešama papildu loģika).
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Dispatcher,Client")] // Pievienojam Client
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientUpdateDto clientDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientController] UpdateClient({ClientId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientController] UpdateClient({ClientId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}", id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != clientDto.ClientId)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientController] UpdateClient({ClientId}): ID nesakrīt ar DTO ID ({DtoClientId}).", id, clientDto.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID pieprasījumā nesakrīt ar objekta ID.");
            }

            try
            {
                if (User.IsInRole("Client"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId)) return Forbid();
                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.ClientId.HasValue || appUser.ClientId.Value != id)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[ClientController] UpdateClient({ClientId}): Klients (AppUserId: {AppUserId}) mēģina atjaunināt svešu profilu.", id, userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Jums nav tiesību atjaunināt šī klienta datus.");
                    }
                }
                // Admin un Dispatcher var atjaunināt jebkuru

                var updatedClient = await _clientService.UpdateClientAsync(clientDto);
                if (updatedClient == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[ClientController] UpdateClient({ClientId}): Klients nav atrasts vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Klients ar ID {id} nav atrasts vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientController] UpdateClient({ClientId}): Klients veiksmīgi atjaunināts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedClient);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[ClientController] Konkurences kļūda UpdateClient({ClientId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Klients, iespējams, tika modificēts vienlaicīgi.");
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientController] Kļūda UpdateClient({ClientId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot klientu: {ex.Message}");
            }
        }

        // DELETE: api/Client/{id}
        // Tikai Admin un Dispatcher var dzēst klientu profilus.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Ja nevar dzēst
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteClient(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientController] DeleteClient({ClientId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _clientService.DeleteClientAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[ClientController] DeleteClient({ClientId}): Klients nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Klients ar ID {id} nav atrasts.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientController] DeleteClient({ClientId}): Klients veiksmīgi dzēsts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Klients ar ID {id} veiksmīgi dzēsts.");
            }
            catch (InvalidOperationException opEx) // Ja serviss izmet, ka nevar dzēst (piem., piesaistītas kravas)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[ClientController] DeleteClient({ClientId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientController] Kļūda DeleteClient({ClientId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot klientu: {ex.Message}");
            }
        }

        // Palīgmetode ModelState kļūdu formatēšanai
        private string ModelStateValuesErrorMessages()
        {
            return string.Join("; ", ModelState.Values
                                .SelectMany(x => x.Errors)
                                .Select(x => x.ErrorMessage));
        }
    }
}