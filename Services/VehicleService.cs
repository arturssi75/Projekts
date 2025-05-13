using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
using Project.Models;
using Project.Models.DTOs; // VehicleViewModel, VehicleCreateDto, VehicleUpdateDto
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss transportlīdzekļu (Vehicle) datu pārvaldībai.
    public class VehicleService
    {
        private readonly TransportContext _context;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(TransportContext context, ILogger<VehicleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visus transportlīdzekļus un konvertē tos uz VehicleViewModel.
        public async Task<IEnumerable<VehicleViewModel>> GetAllVehiclesAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleService] Iegūst visus transportlīdzekļus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var vehicles = await _context.Vehicle
                                       .OrderBy(v => v.LicensePlate)
                                       .ToListAsync();
                return vehicles.Select(v => MapVehicleToViewModel(v)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Kļūda, iegūstot visus transportlīdzekļus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Iegūst konkrētu transportlīdzekli pēc ID un konvertē uz VehicleViewModel.
        public async Task<VehicleViewModel?> GetVehicleByIdAsync(int vehicleId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleService] Iegūst transportlīdzekli ar ID: {VehicleId}", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var vehicle = await _context.Vehicle.FindAsync(vehicleId);
                return vehicle == null ? null : MapVehicleToViewModel(vehicle);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Kļūda, iegūstot transportlīdzekli ar ID: {VehicleId}", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Izveido jaunu transportlīdzekli no VehicleCreateDto.
        public async Task<VehicleViewModel> CreateVehicleAsync(VehicleCreateDto vehicleDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleService] Veido jaunu transportlīdzekli ar numura zīmi: {LicensePlate}", vehicleDto.LicensePlate);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
             if (vehicleDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] CreateVehicleAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(vehicleDto));
            }

            // Pārbaude, vai transportlīdzeklis ar šādu numura zīmi jau neeksistē
            var existingVehicleByPlate = await _context.Vehicle
                .FirstOrDefaultAsync(v => v.LicensePlate == vehicleDto.LicensePlate);
            if (existingVehicleByPlate != null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] Transportlīdzeklis ar numura zīmi '{LicensePlate}' jau eksistē.", vehicleDto.LicensePlate);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Transportlīdzeklis ar numura zīmi '{vehicleDto.LicensePlate}' jau eksistē.");
            }

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8601 // Possible null reference assignment.
            var vehicle = new Vehicle
            {
                LicensePlate = vehicleDto.LicensePlate,
                DriverName = vehicleDto.DriverName
            };
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8601 // Possible null reference assignment.

            _context.Vehicle.Add(vehicle);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleService] Transportlīdzeklis ar ID {VehicleId} veiksmīgi izveidots.", vehicle.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapVehicleToViewModel(vehicle);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Kļūda, veidojot jaunu transportlīdzekli ar numura zīmi: {LicensePlate}", vehicleDto.LicensePlate);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Atjaunina esoša transportlīdzekļa datus no VehicleUpdateDto.
        public async Task<VehicleViewModel?> UpdateVehicleAsync(VehicleUpdateDto vehicleDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleService] Atjaunina transportlīdzekli ar ID: {VehicleId}", vehicleDto.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (vehicleDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] UpdateVehicleAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(vehicleDto));
            }

            var existingVehicle = await _context.Vehicle.FindAsync(vehicleDto.VehicleId);
            if (existingVehicle == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] Transportlīdzeklis ar ID {VehicleId} nav atrasts atjaunināšanai.", vehicleDto.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null;
            }

            // Pārbaude, vai jaunā numura zīme jau nav aizņemta citam transportlīdzeklim
            if (existingVehicle.LicensePlate != vehicleDto.LicensePlate)
            {
                var vehicleWithSamePlate = await _context.Vehicle
                    .FirstOrDefaultAsync(v => v.LicensePlate == vehicleDto.LicensePlate && v.VehicleId != vehicleDto.VehicleId);
                if (vehicleWithSamePlate != null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[VehicleService] Mēģinājums atjaunināt numura zīmi uz '{NewPlate}', kas jau eksistē citam transportlīdzeklim.", vehicleDto.LicensePlate);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    throw new InvalidOperationException($"Transportlīdzeklis ar numura zīmi '{vehicleDto.LicensePlate}' jau eksistē citam transportlīdzeklim.");
                }
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            existingVehicle.LicensePlate = vehicleDto.LicensePlate;
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning disable CS8601 // Possible null reference assignment.
            existingVehicle.DriverName = vehicleDto.DriverName;
#pragma warning restore CS8601 // Possible null reference assignment.

            _context.Entry(existingVehicle).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleService] Transportlīdzeklis ar ID {VehicleId} veiksmīgi atjaunināts.", existingVehicle.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapVehicleToViewModel(existingVehicle);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Konkurences kļūda, atjauninot transportlīdzekli ar ID: {VehicleId}", vehicleDto.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Kļūda, atjauninot transportlīdzekli ar ID: {VehicleId}", vehicleDto.VehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Dzēš transportlīdzekli pēc ID.
        public async Task<bool> DeleteVehicleAsync(int vehicleId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[VehicleService] Mēģina dzēst transportlīdzekli ar ID: {VehicleId}", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var vehicle = await _context.Vehicle.FindAsync(vehicleId);

            if (vehicle == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] Transportlīdzeklis ar ID {VehicleId} nav atrasts dzēšanai.", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false;
            }

            // Pārbaude, vai transportlīdzeklis ir piesaistīts kādai aktīvai kravai
            // (Šī loģika ir jāprecizē atkarībā no tā, kā Vehicle tiek sasaistīts ar Cargo.
            // Pašreizējā modeļu struktūrā Vehicle nav tiešas saites UZ Cargo, bet Cargo varētu saturēt VehicleId)
            // Pieņemsim, ka Cargo modelī ir VehicleId:
            bool isUsedInCargo = await _context.Cargo.AnyAsync(c => c.VehicleId == vehicleId && 
                                                               (c.Status == CargoStatus.InTransit || c.Status == CargoStatus.Pending || c.Status == CargoStatus.RouteAssigned));
            if (isUsedInCargo)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[VehicleService] Nevar dzēst transportlīdzekli ar ID {VehicleId}, jo tas ir piesaistīts aktīvai kravai.", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Nevar dzēst transportlīdzekli '{vehicle.LicensePlate}', jo tas ir piesaistīts aktīvai(-ām) kravai(-ām).");
            }


            _context.Vehicle.Remove(vehicle);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[VehicleService] Transportlīdzeklis ar ID {VehicleId} veiksmīgi dzēsts.", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[VehicleService] Kļūda, dzēšot transportlīdzekli ar ID: {VehicleId}", vehicleId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Palīgmetode Vehicle konvertēšanai uz VehicleViewModel
        private VehicleViewModel MapVehicleToViewModel(Vehicle vehicle)
        {
            return new VehicleViewModel
            {
                VehicleId = vehicle.VehicleId,
                LicensePlate = vehicle.LicensePlate,
                DriverName = vehicle.DriverName
            };
        }
    }
}