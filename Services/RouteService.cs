using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
// Ja alias ir definēts globāli vai Project.Models, tad Project.Models.Route var izmantot tieši
using DbRoute = Project.Models.Route; // Alias, lai izvairītos no konfliktiem
using Project.Models.DTOs; // RouteViewModel, RouteCreateDto, RouteUpdateDto
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss maršrutu (Route) datu pārvaldībai.
    public class RouteService
    {
        private readonly TransportContext _context;
        private readonly ILogger<RouteService> _logger;

        public RouteService(TransportContext context, ILogger<RouteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visus maršrutus un konvertē tos uz RouteViewModel.
        public async Task<IEnumerable<RouteViewModel>> GetAllRoutesAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteService] Iegūst visus maršrutus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var routes = await _context.Route // Izmantojam DbSet<DbRoute> Route
                                     .OrderBy(r => r.StartPoint).ThenBy(r => r.EndPoint)
                                     .ToListAsync();
                return routes.Select(r => MapRouteToViewModel(r)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Kļūda, iegūstot visus maršrutus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Iegūst konkrētu maršrutu pēc ID un konvertē uz RouteViewModel.
        public async Task<RouteViewModel?> GetRouteByIdAsync(int routeId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteService] Iegūst maršrutu ar ID: {RouteId}", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var route = await _context.Route.FindAsync(routeId);
                return route == null ? null : MapRouteToViewModel(route);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Kļūda, iegūstot maršrutu ar ID: {RouteId}", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Izveido jaunu maršrutu no RouteCreateDto.
        public async Task<RouteViewModel> CreateRouteAsync(RouteCreateDto routeDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteService] Veido jaunu maršrutu: {StartPoint} -> {EndPoint}", routeDto.StartPoint, routeDto.EndPoint);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (routeDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteService] CreateRouteAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(routeDto));
            }

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8601 // Possible null reference assignment.
            var route = new DbRoute // Izmantojam alias DbRoute
            {
                StartPoint = routeDto.StartPoint,
                EndPoint = routeDto.EndPoint,
                WayPoints = routeDto.WayPoints ?? new List<string>(),
                EstimatedTime = routeDto.EstimatedTime
            };
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8601 // Possible null reference assignment.

            _context.Route.Add(route);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteService] Maršruts ar ID {RouteId} veiksmīgi izveidots.", route.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapRouteToViewModel(route);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Kļūda, veidojot jaunu maršrutu: {StartPoint} -> {EndPoint}", routeDto.StartPoint, routeDto.EndPoint);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Atjaunina esoša maršruta datus no RouteUpdateDto.
        public async Task<RouteViewModel?> UpdateRouteAsync(RouteUpdateDto routeDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteService] Atjaunina maršrutu ar ID: {RouteId}", routeDto.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (routeDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteService] UpdateRouteAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(routeDto));
            }

            var existingRoute = await _context.Route.FindAsync(routeDto.RouteId);
            if (existingRoute == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteService] Maršruts ar ID {RouteId} nav atrasts atjaunināšanai.", routeDto.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null;
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            existingRoute.StartPoint = routeDto.StartPoint;
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning disable CS8601 // Possible null reference assignment.
            existingRoute.EndPoint = routeDto.EndPoint;
#pragma warning restore CS8601 // Possible null reference assignment.
            existingRoute.WayPoints = routeDto.WayPoints ?? new List<string>();
            existingRoute.EstimatedTime = routeDto.EstimatedTime;

            _context.Entry(existingRoute).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteService] Maršruts ar ID {RouteId} veiksmīgi atjaunināts.", existingRoute.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapRouteToViewModel(existingRoute);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Konkurences kļūda, atjauninot maršrutu ar ID: {RouteId}", routeDto.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Kļūda, atjauninot maršrutu ar ID: {RouteId}", routeDto.RouteId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Dzēš maršrutu pēc ID.
        public async Task<bool> DeleteRouteAsync(int routeId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[RouteService] Mēģina dzēst maršrutu ar ID: {RouteId}", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var route = await _context.Route.FindAsync(routeId);

            if (route == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteService] Maršruts ar ID {RouteId} nav atrasts dzēšanai.", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false;
            }

            // Pārbaude, vai maršruts ir piesaistīts kādai kravai
            bool isRouteUsed = await _context.Cargo.AnyAsync(c => c.RouteId == routeId);
            if (isRouteUsed)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[RouteService] Nevar dzēst maršrutu ar ID {RouteId}, jo tas ir piesaistīts vienai vai vairākām kravām.", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Nevar dzēst maršrutu, jo tas ir piesaistīts kravām. Vispirms noņemiet šo maršrutu no visām kravām.");
            }

            _context.Route.Remove(route);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[RouteService] Maršruts ar ID {RouteId} veiksmīgi dzēsts.", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[RouteService] Kļūda, dzēšot maršrutu ar ID: {RouteId}", routeId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Palīgmetode DbRoute konvertēšanai uz RouteViewModel
        private RouteViewModel MapRouteToViewModel(DbRoute route)
        {
            return new RouteViewModel
            {
                RouteId = route.RouteId,
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                WayPoints = route.WayPoints ?? new List<string>(), // Nodrošina, ka saraksts nav null
                EstimatedTime = route.EstimatedTime
            };
        }
    }
}