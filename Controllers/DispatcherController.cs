using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Nepieciešams DbUpdateConcurrencyException
using Microsoft.Extensions.Logging;
using Project.Models;
using Project.Models.DTOs;
using Project.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DispatcherController : ControllerBase
    {
        private readonly DispatcherService _dispatcherService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DispatcherController> _logger;

        public DispatcherController(
            DispatcherService dispatcherService,
            UserManager<ApplicationUser> userManager,
            ILogger<DispatcherController> logger)
        {
            _dispatcherService = dispatcherService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DispatcherViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllDispatchers()
        {
            ;
            try
            {
                var dispatchers = await _dispatcherService.GetAllDispatchersAsync();
                return Ok(dispatchers);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherController] Kļūda GetAllDispatchers metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot dispečerus.");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatcherViewModel))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDispatcherById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherController] GetDispatcherById({DispatcherId}) izsaukts no lietotāja: {UserIdentityName}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                if (User.IsInRole("Dispatcher"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId)) return Forbid("Lietotāja ID nav atrasts tokenā.");

                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.DispatcherId.HasValue || appUser.DispatcherId.Value != id)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[DispatcherController] GetDispatcherById({DispatcherId}): Dispečers (AppUserId: {AppUserId}) mēģina piekļūt svešam profilam vai nav piesaistīts DispatcherId.", id, userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Jums nav tiesību piekļūt šī dispečera datiem.");
                    }
                }

                var dispatcherViewModel = await _dispatcherService.GetDispatcherByIdAsync(id);
                if (dispatcherViewModel == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DispatcherController] GetDispatcherById({DispatcherId}): Dispečers nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Dispečers ar ID {id} nav atrasts.");
                }
                return Ok(dispatcherViewModel);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherController] Kļūda GetDispatcherById({DispatcherId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot dispečeru.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DispatcherViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDispatcher([FromBody] DispatcherCreateDto dispatcherDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherController] CreateDispatcher izsaukts no lietotāja: {UserIdentityName} ar datiem: {@DispatcherDto}", User.Identity?.Name, dispatcherDto);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherController] CreateDispatcher: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdDispatcher = await _dispatcherService.CreateDispatcherAsync(dispatcherDto);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherController] CreateDispatcher: Dispečers ar ID {SenderId} veiksmīgi izveidots.", createdDispatcher.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return CreatedAtAction(nameof(GetDispatcherById), new { id = createdDispatcher.SenderId }, createdDispatcher);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherController] Kļūda CreateDispatcher metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot dispečeru: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatcherViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateDispatcher(int id, [FromBody] DispatcherUpdateDto dispatcherDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherController] UpdateDispatcher({PassedId}) izsaukts no lietotāja: {UserIdentityName} ar datiem: {@DispatcherDto}", id, User.Identity?.Name, dispatcherDto);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherController] UpdateDispatcher({PassedId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}", id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != dispatcherDto.SenderId)
            {
                // Izlabots logotnes ziņojums, lai atbilstu argumentiem.
                // {PassedId} ir tas, kas nāk no URL. {DtoSenderId} ir tas, kas ir DTO.
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherController] UpdateDispatcher: ID pieprasījumā ({PassedId}) nesakrīt ar DTO ID ({DtoSenderId}).", id, dispatcherDto.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID pieprasījumā nesakrīt ar objekta ID.");
            }

            try
            {
                if (User.IsInRole("Dispatcher"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId)) return Forbid();
                    var appUser = await _userManager.FindByIdAsync(userId);
                    if (appUser == null || !appUser.DispatcherId.HasValue || appUser.DispatcherId.Value != id)
                    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[DispatcherController] UpdateDispatcher({PassedId}): Dispečers (AppUserId: {AppUserId}) mēģina atjaunināt svešu profilu.", id, userId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        return Forbid("Jums nav tiesību atjaunināt šī dispečera datus.");
                    }
                }

                var updatedDispatcher = await _dispatcherService.UpdateDispatcherAsync(dispatcherDto);
                if (updatedDispatcher == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DispatcherController] UpdateDispatcher({PassedId}): Dispečers nav atrasts vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Dispečers ar ID {id} nav atrasts vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherController] UpdateDispatcher({PassedId}): Dispečers veiksmīgi atjaunināts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedDispatcher);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[DispatcherController] Konkurences kļūda UpdateDispatcher({PassedId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Dispečera dati, iespējams, tika modificēti vienlaicīgi.");
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherController] Kļūda UpdateDispatcher({PassedId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot dispečeru: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDispatcher(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherController] DeleteDispatcher({DispatcherId}) izsaukts no lietotāja: {UserIdentityName}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _dispatcherService.DeleteDispatcherAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DispatcherController] DeleteDispatcher({DispatcherId}): Dispečers nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Dispečers ar ID {id} nav atrasts.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherController] DeleteDispatcher({DispatcherId}): Dispečers veiksmīgi dzēsts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Dispečers ar ID {id} veiksmīgi dzēsts.");
            }
            catch (InvalidOperationException opEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[DispatcherController] DeleteDispatcher({DispatcherId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherController] Kļūda DeleteDispatcher({DispatcherId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot dispečeru: {ex.Message}");
            }
        }

        private string ModelStateValuesErrorMessages()
        {
            return string.Join("; ", ModelState.Values
                                .SelectMany(x => x.Errors)
                                .Select(x => x.ErrorMessage));
        }
    }
}
