using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.DTOs; // RouteViewModel, RouteCreateDto, RouteUpdateDto
using Project.Services;
// Izmantojam alias, ja nepieciešams
using DbRoute = Project.Models.Route;

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Dispatcher")] // Maršrutus pārvalda tikai Admin un Dispatcher
    public class RouteController : ControllerBase
    {
        private readonly RouteService _routeService;
        private readonly ILogger<RouteController> _logger;

        public RouteController(RouteService routeService, ILogger<RouteController> logger)
        {
            _routeService = routeService;
            _logger = logger;
        }

        // GET: api/Route
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<RouteViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllRoutes()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteController] GetAllRoutes izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var routes = await _routeService.GetAllRoutesAsync();
                return Ok(routes);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteController] Kļūda GetAllRoutes metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot maršrutus.");
            }
        }

        // GET: api/Route/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RouteViewModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRouteById(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteController] GetRouteById({RouteId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var routeViewModel = await _routeService.GetRouteByIdAsync(id);
                if (routeViewModel == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[RouteController] GetRouteById({RouteId}): Maršruts nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Maršruts ar ID {id} nav atrasts.");
                }
                return Ok(routeViewModel);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteController] Kļūda GetRouteById({RouteId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, "Iekšēja servera kļūda, iegūstot maršrutu.");
            }
        }

        // POST: api/Route
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(RouteViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRoute([FromBody] RouteCreateDto routeDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteController] CreateRoute izsaukts no lietotāja: {User}", User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteController] CreateRoute: Modelis nav derīgs. Kļūdas: {ModelStateErrors}", ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            try
            {
                var createdRoute = await _routeService.CreateRouteAsync(routeDto);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteController] CreateRoute: Maršruts ar ID {RouteId} veiksmīgi izveidots.", createdRoute.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.RouteId }, createdRoute);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteController] Kļūda CreateRoute metodē.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, veidojot maršrutu: {ex.Message}");
            }
        }

        // PUT: api/Route/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RouteViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateRoute(int id, [FromBody] RouteUpdateDto routeDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteController] UpdateRoute({RouteId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (!ModelState.IsValid)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteController] UpdateRoute({RouteId}): Modelis nav derīgs. Kļūdas: {ModelStateErrors}",id, ModelStateValuesErrorMessages());
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(ModelState);
            }
            if (id != routeDto.RouteId)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA2017 // Parameter count mismatch
                _logger.LogWarning("[RouteController] UpdateRoute({RouteId}): ID pieprasījumā ({RequestId}) nesakrīt ar DTO ID ({DtoRouteId}).", id, routeDto.RouteId);
#pragma warning restore CA2017 // Parameter count mismatch
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest("ID pieprasījumā nesakrīt ar objekta ID.");
            }

            try
            {
                var updatedRoute = await _routeService.UpdateRouteAsync(routeDto);
                if (updatedRoute == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[RouteController] UpdateRoute({RouteId}): Maršruts nav atrasts vai neizdevās atjaunināt.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Maršruts ar ID {id} nav atrasts vai neizdevās atjaunināt.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteController] UpdateRoute({RouteId}): Maršruts veiksmīgi atjaunināts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok(updatedRoute);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(dbEx, "[RouteController] Konkurences kļūda UpdateRoute({RouteId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status409Conflict, "Datu konflikts. Maršruts, iespējams, tika modificēts vienlaicīgi.");
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteController] Kļūda UpdateRoute({RouteId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, atjauninot maršrutu: {ex.Message}");
            }
        }

        // DELETE: api/Route/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Ja nevar dzēst
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRoute(int id)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteController] DeleteRoute({RouteId}) izsaukts no lietotāja: {User}", id, User.Identity?.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var success = await _routeService.DeleteRouteAsync(id);
                if (!success)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[RouteController] DeleteRoute({RouteId}): Maršruts nav atrasts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return NotFound($"Maršruts ar ID {id} nav atrasts.");
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteController] DeleteRoute({RouteId}): Maršruts veiksmīgi dzēsts.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return Ok($"Maršruts ar ID {id} veiksmīgi dzēsts.");
            }
            catch (InvalidOperationException opEx) // Ja serviss izmet, ka nevar dzēst (piem., piesaistīts kravai)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning(opEx, "[RouteController] DeleteRoute({RouteId}): Operācijas kļūda.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return BadRequest(new { message = opEx.Message });
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteController] Kļūda DeleteRoute({RouteId}) metodē.", id);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return StatusCode(StatusCodes.Status500InternalServerError, $"Iekšēja servera kļūda, dzēšot maršrutu: {ex.Message}");
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