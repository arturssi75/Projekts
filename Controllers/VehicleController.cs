using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Nepieciešams DbUpdateConcurrencyException
using Microsoft.Extensions.Logging;
using Project.Models.DTOs;
using Project.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public class VehicleController : ControllerBase
    {
        private readonly VehicleService _vehicleService;
        private readonly ILogger<VehicleController> _logger;

        public VehicleController(VehicleService vehicleService, ILogger<VehicleController> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VehicleViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllVehicles()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleController] GetAllVehicles izsaukts no lietotāja: {UserIdentityName}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var vehicles = await _vehicleService.GetAllVehiclesAsync();
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleController] Kļūda GetAllVehicles metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot transportlīdzekļus.");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VehicleViewModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVehicleById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleController] GetVehicleById({VehicleId}) izsaukts no lietotāja: {UserIdentityName}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var vehicleViewModel = await _vehicleService.GetVehicleByIdAsync(id);
                if (vehicleViewModel == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[VehicleController] GetVehicleById({VehicleId}): Transportlīdzeklis nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Transportlīdzeklis ar ID {id} nav atrasts.");
                }
                return Ok(vehicleViewModel);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleController] Kļūda GetVehicleById({VehicleId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot transportlīdzekli.");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VehicleViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateVehicle([FromBody] VehicleCreateDto vehicleDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleController] CreateVehicle izsaukts no lietotāja: {UserIdentityName} ar datiem: {@VehicleDto}", User.Identity?.Name, vehicleDto);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleController] CreateVehicle: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdVehicle = await _vehicleService.CreateVehicleAsync(vehicleDto);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleController] CreateVehicle: Transportlīdzeklis ar ID {VehicleId} veiksmīgi izveidots.", createdVehicle.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return CreatedAtAction(nameof(GetVehicleById), new { id = createdVehicle.VehicleId }, createdVehicle);
            }
            catch (InvalidOperationException opEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[VehicleController] CreateVehicle: Operācijas kļūda (piem., dublikāts numura zīmei).");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleController] Kļūda CreateVehicle metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot transportlīdzekli: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VehicleViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] VehicleUpdateDto vehicleDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleController] UpdateVehicle({PassedId}) izsaukts no lietotāja: {UserIdentityName} ar datiem: {@VehicleDto}", id, User.Identity?.Name, vehicleDto);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleController] UpdateVehicle({PassedId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}", id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != vehicleDto.VehicleId)
            {
                // Izlabots logotnes ziņojums, lai atbilstu argumentiem.
                // {PassedId} ir tas, kas nāk no URL. {DtoVehicleId} ir tas, kas ir DTO.
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleController] UpdateVehicle: ID pieprasījumā ({PassedId}) nesakrīt ar DTO ID ({DtoVehicleId}).", id, vehicleDto.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID pieprasījumā nesakrīt ar objekta ID.");
            }

            try
            {
                var updatedVehicle = await _vehicleService.UpdateVehicleAsync(vehicleDto);
                if (updatedVehicle == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[VehicleController] UpdateVehicle({PassedId}): Transportlīdzeklis nav atrasts vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Transportlīdzeklis ar ID {id} nav atrasts vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleController] UpdateVehicle({PassedId}): Transportlīdzeklis veiksmīgi atjaunināts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedVehicle);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[VehicleController] Konkurences kļūda UpdateVehicle({PassedId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Transportlīdzekļa dati, iespējams, tika modificēti vienlaicīgi.");
            }
            catch (InvalidOperationException opEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[VehicleController] UpdateVehicle({PassedId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleController] Kļūda UpdateVehicle({PassedId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot transportlīdzekli: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleController] DeleteVehicle({VehicleId}) izsaukts no lietotāja: {UserIdentityName}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _vehicleService.DeleteVehicleAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[VehicleController] DeleteVehicle({VehicleId}): Transportlīdzeklis nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Transportlīdzeklis ar ID {id} nav atrasts.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleController] DeleteVehicle({VehicleId}): Transportlīdzeklis veiksmīgi dzēsts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Transportlīdzeklis ar ID {id} veiksmīgi dzēsts.");
            }
            catch (InvalidOperationException opEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[VehicleController] DeleteVehicle({VehicleId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleController] Kļūda DeleteVehicle({VehicleId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot transportlīdzekli: {ex.Message}");
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
