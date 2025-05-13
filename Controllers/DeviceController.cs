using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Authorize] 
    public class DeviceController : ControllerBase
    {
        private readonly DeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(DeviceService deviceService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        // GET: api/Device
        [HttpGet]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DeviceViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDevices()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] GetAllDevices izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();
                return Ok(devices);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda GetAllDevices metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot ierīces.");
            }
        }

        // GET: api/Device/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Dispatcher,Client")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeviceViewModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeviceById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] GetDeviceById({DeviceId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var deviceViewModel = await _deviceService.GetDeviceByIdAsync(id);
                if (deviceViewModel == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DeviceController] GetDeviceById({DeviceId}): Ierīce nav atrasta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Ierīce ar ID {id} nav atrasta.");
                }
                return Ok(deviceViewModel);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda GetDeviceById({DeviceId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot ierīci.");
            }
        }

        // POST: api/Device
        [HttpPost]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DeviceViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceCreateDto deviceDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] CreateDevice izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceController] CreateDevice: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdDevice = await _deviceService.CreateDeviceAsync(deviceDto);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceController] CreateDevice: Ierīce ar ID {DeviceId} veiksmīgi izveidota.", createdDevice.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return CreatedAtAction(nameof(GetDeviceById), new { id = createdDevice.DeviceId }, createdDevice);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda CreateDevice metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot ierīci: {ex.Message}");
            }
        }

        // PUT: api/Device/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeviceViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] DeviceUpdateDto deviceDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] UpdateDevice({DeviceId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceController] UpdateDevice({DeviceId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}", id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != deviceDto.DeviceId)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceController] UpdateDevice({DeviceId}): ID nesakrīt ar DTO ID ({DtoDeviceId}).", id, deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID pieprasījumā nesakrīt ar objekta ID.");
            }

            try
            {
                var updatedDevice = await _deviceService.UpdateDeviceAsync(deviceDto);
                if (updatedDevice == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DeviceController] UpdateDevice({DeviceId}): Ierīce nav atrasta vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Ierīce ar ID {id} nav atrasta vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceController] UpdateDevice({DeviceId}): Ierīce veiksmīgi atjaunināta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedDevice);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[DeviceController] Konkurences kļūda UpdateDevice({DeviceId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Ierīce, iespējams, tika modificēta vienlaicīgi. Lūdzu, atsvaidziniet datus un mēģiniet vēlreiz.");
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda UpdateDevice({DeviceId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot ierīci: {ex.Message}");
            }
        }

        // DELETE: api/Device/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Dispatcher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDevice(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] DeleteDevice({DeviceId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _deviceService.DeleteDeviceAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[DeviceController] DeleteDevice({DeviceId}): Ierīce nav atrasta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Ierīce ar ID {id} nav atrasta.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceController] DeleteDevice({DeviceId}): Ierīce veiksmīgi dzēsta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Ierīce ar ID {id} veiksmīgi dzēsta.");
            }
            catch (InvalidOperationException opEx) 
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[DeviceController] DeleteDevice({DeviceId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda DeleteDevice({DeviceId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot ierīci: {ex.Message}");
            }
        }

        // JAUNS API GALAPUNKTS: Iegūst ierīces vēsturi
        // GET: api/Device/{id}/history?startDate=2023-01-01&endDate=2023-01-31
        [HttpGet("{id}/history")]
        [Authorize(Roles = "Admin,Dispatcher,Client")] // Pielāgojiet autorizāciju pēc nepieciešamības
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DeviceHistoryPointDto>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeviceHistory(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceController] GetDeviceHistory({DeviceId}) izsaukts. Periods: {StartDate} - {EndDate}. Lietotājs: {User}", id, startDate, endDate, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                // TODO: Pievienot papildu autorizācijas loģiku, ja Klients drīkst redzēt tikai savu ierīču vēsturi.
                // Piemēram, pārbaudīt, vai ierīce ar 'id' ir piesaistīta kādai no pašreizējā klienta kravām.
                // Šobrīd pieņemam, ka, ja lietotājam ir piekļuve šim kontrolierim (atbilstoši lomai), viņš drīkst redzēt.

                var deviceExists = await _deviceService.GetDeviceByIdAsync(id); // Pārbaudām, vai ierīce vispār eksistē
                if (deviceExists == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                     _logger.LogWarning("[DeviceController] GetDeviceHistory({DeviceId}): Ierīce nav atrasta.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Ierīce ar ID {id} nav atrasta.");
                }

                var history = await _deviceService.GetDeviceHistoryAsync(id, startDate, endDate);
                return Ok(history);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceController] Kļūda GetDeviceHistory({DeviceId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot ierīces vēsturi.");
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
