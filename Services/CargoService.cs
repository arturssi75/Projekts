using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // ILogger
using Project.Data;
using Project.Models;
using Project.Models.DTOs; // Nepieciešams DTOs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss kravu (Cargo) datu pārvaldībai.
    // Nodrošina metodes kravu izveidei, nolasīšanai, atjaunināšanai un dzēšanai (CRUD),
    // kā arī specifiskas vaicājuma metodes.
    // Sadarbojas ar TransportContext, lai veiktu datubāzes operācijas.
    public class CargoService
    {
        private readonly TransportContext _context;
        private readonly ILogger<CargoService> _logger;

        public CargoService(TransportContext context, ILogger<CargoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visas kravas un konvertē tās uz CargoViewModel.
        // Iekļauj saistītās entītijas (Sūtītāju, Saņēmēju, Maršrutu, Ierīces).
        public async Task<IEnumerable<CargoViewModel>> GetAllCargosAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Iegūst visas kravas.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var cargos = await _context.Cargo
                    .Include(c => c.Sender)    // Dispečers
                    .Include(c => c.Receiver)  // Klients
                    .Include(c => c.Route)
                    .Include(c => c.Devices)    // Piesaistītās ierīces
                    .OrderByDescending(c => c.CreatedAt) // Sakārtojam pēc izveides datuma
                    .ToListAsync();

                // Konvertējam uz ViewModel
                return cargos.Select(c => MapCargoToViewModel(c)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, iegūstot visas kravas.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw; // Pārmet kļūdu tālāk
            }
        }

        // Iegūst kravas, kas pieder konkrētam klientam (ClientId).
        public async Task<IEnumerable<CargoViewModel>> GetCargosByClientIdAsync(int clientId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Iegūst kravas klientam ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var cargos = await _context.Cargo
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .Include(c => c.Route)
                    .Include(c => c.Devices)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return cargos.Select(c => MapCargoToViewModel(c)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, iegūstot kravas klientam ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }


        // Iegūst konkrētu kravu pēc ID un konvertē to uz CargoViewModel.
        public async Task<CargoViewModel?> GetCargoByIdAsync(int cargoId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Iegūst kravu ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var cargo = await _context.Cargo
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .Include(c => c.Route)
                    .Include(c => c.Devices)
                    .FirstOrDefaultAsync(c => c.CargoId == cargoId);

                return cargo == null ? null : MapCargoToViewModel(cargo);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, iegūstot kravu ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Izveido jaunu kravu no CargoCreateDto.
        // Piesaista norādītās ierīces.
        public async Task<CargoViewModel?> CreateCargoAsync(CargoCreateDto cargoDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Veido jaunu kravu.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (cargoDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoService] CreateCargoAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(cargoDto));
            }

            // Pārbaudām, vai saistītās entītijas eksistē
            if (!await _context.Dispatcher.AnyAsync(d => d.SenderId == cargoDto.SenderId))
                throw new ArgumentException($"Sūtītājs (Dispatcher) ar ID {cargoDto.SenderId} nav atrasts.", nameof(cargoDto.SenderId));
            if (!await _context.Client.AnyAsync(cl => cl.ClientId == cargoDto.ClientId))
                throw new ArgumentException($"Klients (Client) ar ID {cargoDto.ClientId} nav atrasts.", nameof(cargoDto.ClientId));
            if (!await _context.Route.AnyAsync(r => r.RouteId == cargoDto.RouteId))
                throw new ArgumentException($"Maršruts (Route) ar ID {cargoDto.RouteId} nav atrasts.", nameof(cargoDto.RouteId));

            var cargo = new Cargo
            {
                Status = cargoDto.Status,
                SenderId = cargoDto.SenderId,
                ClientId = cargoDto.ClientId,
                RouteId = cargoDto.RouteId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Devices = new List<Device>() // Inicializējam ierīču sarakstu
            };

            _context.Cargo.Add(cargo);

            try
            {
                // Saglabājam kravu, lai iegūtu CargoId pirms ierīču piesaistes
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoService] Krava ar ID {CargoId} sākotnēji saglabāta.", cargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

                // Piesaistām ierīces, ja tādas norādītas
                if (cargoDto.DeviceIds != null && cargoDto.DeviceIds.Any())
                {
                    var devicesToAttach = await _context.Device
                        .Where(d => cargoDto.DeviceIds.Contains(d.DeviceId))
                        .ToListAsync();

                    if (devicesToAttach.Count != cargoDto.DeviceIds.Count)
                    {
                        var foundDeviceIds = devicesToAttach.Select(d => d.DeviceId);
                        var notFoundDeviceIds = cargoDto.DeviceIds.Except(foundDeviceIds);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                        _logger.LogWarning("[CargoService] Dažas ierīces netika atrastas piesaistei: {NotFoundDeviceIds}", string.Join(", ", notFoundDeviceIds));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                        // Šeit varētu izmest kļūdu vai tikai logot, atkarībā no prasībām
                        // throw new ArgumentException($"Nevarēja atrast visas norādītās ierīces. Trūkst ID: {string.Join(", ", notFoundDeviceIds)}");
                    }
                    
                    foreach (var device in devicesToAttach)
                    {
                        device.CargoId = cargo.CargoId; // Piesaista ierīci kravai
                        _context.Entry(device).State = EntityState.Modified;
                    }
                    cargo.Devices = devicesToAttach; // Atjaunina kravas navigācijas īpašību
                    await _context.SaveChangesAsync(); // Saglabā ierīču izmaiņas
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[CargoService] Ierīces {DeviceIds} piesaistītas kravai {CargoId}.", string.Join(", ", devicesToAttach.Select(d=>d.DeviceId)), cargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                
                // Ielādējam pilno kravas objektu ar visām relācijām, lai atgrieztu ViewModel
                var createdCargoWithIncludes = await _context.Cargo
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .Include(c => c.Route)
                    .Include(c => c.Devices)
                    .FirstOrDefaultAsync(c => c.CargoId == cargo.CargoId);

                return createdCargoWithIncludes == null ? null : MapCargoToViewModel(createdCargoWithIncludes);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, veidojot jaunu kravu.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                // Ja kaut kas nogāja greizi pēc sākotnējās saglabāšanas, varētu apsvērt kravas dzēšanu (rollback)
                if (cargo.CargoId > 0 && !_context.Cargo.Local.Any(e => e.CargoId == cargo.CargoId && e.Devices.Any())) // Pārbaudam vai krava jau nav pievienota ar ierīcēm
                {
                     var entry = _context.Cargo.FirstOrDefault(e => e.CargoId == cargo.CargoId);
                     if (entry != null) _context.Cargo.Remove(entry);
                     await _context.SaveChangesAsync();
                }
                throw;
            }
        }

        // Atjaunina esošu kravu no CargoUpdateDto.
        // Apstrādā arī ierīču saraksta izmaiņas.
        public async Task<CargoViewModel?> UpdateCargoAsync(CargoUpdateDto cargoDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Atjaunina kravu ar ID: {CargoId}", cargoDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (cargoDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoService] UpdateCargoAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(cargoDto));
            }

            var existingCargo = await _context.Cargo
                .Include(c => c.Devices) // Svarīgi iekļaut esošās ierīces, lai tās varētu atsaistīt
                .FirstOrDefaultAsync(c => c.CargoId == cargoDto.CargoId);

            if (existingCargo == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoService] Krava ar ID {CargoId} nav atrasta atjaunināšanai.", cargoDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null; // Krava nav atrasta
            }

            // Pārbaudām, vai saistītās entītijas eksistē
            if (!await _context.Dispatcher.AnyAsync(d => d.SenderId == cargoDto.SenderId))
                throw new ArgumentException($"Sūtītājs (Dispatcher) ar ID {cargoDto.SenderId} nav atrasts.", nameof(cargoDto.SenderId));
            if (!await _context.Client.AnyAsync(cl => cl.ClientId == cargoDto.ClientId))
                throw new ArgumentException($"Klients (Client) ar ID {cargoDto.ClientId} nav atrasts.", nameof(cargoDto.ClientId));
            if (!await _context.Route.AnyAsync(r => r.RouteId == cargoDto.RouteId))
                throw new ArgumentException($"Maršruts (Route) ar ID {cargoDto.RouteId} nav atrasts.", nameof(cargoDto.RouteId));

            // Atjauninam kravas pamatinformāciju
            existingCargo.Status = cargoDto.Status;
            existingCargo.SenderId = cargoDto.SenderId;
            existingCargo.ClientId = cargoDto.ClientId;
            existingCargo.RouteId = cargoDto.RouteId;
            existingCargo.UpdatedAt = DateTime.UtcNow;

            // Apstrādājam ierīču saraksta izmaiņas
            // 1. Atsaistām visas pašreizējās ierīces, kas nav jaunajā sarakstā (vai visas, ja tā ir loģika)
            var currentDeviceIds = existingCargo.Devices.Select(d => d.DeviceId).ToList();
            var newDeviceIds = cargoDto.DeviceIds ?? new List<int>();

            var devicesToUnassign = existingCargo.Devices
                .Where(d => !newDeviceIds.Contains(d.DeviceId))
                .ToList();

            foreach (var device in devicesToUnassign)
            {
                device.CargoId = null; // Atsaista ierīci
                _context.Entry(device).State = EntityState.Modified;
                existingCargo.Devices.Remove(device); // Noņem no kravas navigācijas kolekcijas
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogInformation("[CargoService] Ierīce ar ID {DeviceId} atsaistīta no kravas {CargoId}.", device.DeviceId, existingCargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            }

            // 2. Piesaistām jaunās ierīces, kas vēl nav piesaistītas
            var devicesToAssignIds = newDeviceIds.Except(currentDeviceIds).ToList();
            if (devicesToAssignIds.Any())
            {
                var devicesToActuallyAssign = await _context.Device
                    .Where(d => devicesToAssignIds.Contains(d.DeviceId))
                    .ToListAsync();
                
                if (devicesToActuallyAssign.Count != devicesToAssignIds.Count)
                {
                    var foundDeviceIds = devicesToActuallyAssign.Select(d => d.DeviceId);
                    var notFoundDeviceIds = devicesToAssignIds.Except(foundDeviceIds);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("[CargoService] Dažas ierīces netika atrastas piesaistei atjaunināšanas laikā: {NotFoundDeviceIds}", string.Join(", ", notFoundDeviceIds));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    // throw new ArgumentException($"Nevarēja atrast visas norādītās jaunās ierīces. Trūkst ID: {string.Join(", ", notFoundDeviceIds)}");
                }

                foreach (var device in devicesToActuallyAssign)
                {
                    // Pārbaudam, vai ierīce jau nav piesaistīta citai kravai (ja biznesa loģika to neļauj)
                    // if (device.CargoId.HasValue && device.CargoId != existingCargo.CargoId) {
                    // _logger.LogWarning($"Ierīce {device.DeviceId} jau ir piesaistīta kravai {device.CargoId}.");
                    // throw new InvalidOperationException($"Ierīce ar ID {device.DeviceId} jau ir piesaistīta citai kravai.");
                    // }
                    device.CargoId = existingCargo.CargoId;
                    _context.Entry(device).State = EntityState.Modified;
                    existingCargo.Devices.Add(device); // Pievieno navigācijas kolekcijai
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[CargoService] Ierīce ar ID {DeviceId} piesaistīta kravai {CargoId} atjaunināšanas laikā.", device.DeviceId, existingCargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
            }
            
            _context.Entry(existingCargo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoService] Krava ar ID {CargoId} veiksmīgi atjaunināta.", existingCargo.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                
                // Ielādējam atjaunināto kravu ar visām relācijām
                var updatedCargoWithIncludes = await _context.Cargo
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .Include(c => c.Route)
                    .Include(c => c.Devices)
                    .FirstOrDefaultAsync(c => c.CargoId == existingCargo.CargoId);

                return updatedCargoWithIncludes == null ? null : MapCargoToViewModel(updatedCargoWithIncludes);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Konkurences kļūda, atjauninot kravu ar ID: {CargoId}", cargoDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw; // Pārmet tālāk, lai kontrolieris varētu apstrādāt ar 409 Conflict
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, atjauninot kravu ar ID: {CargoId}", cargoDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Dzēš kravu pēc ID.
        // Pārbauda, vai kravu drīkst dzēst (piemēram, ja tā nav aktīva).
        public async Task<bool> DeleteCargoAsync(int cargoId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[CargoService] Mēģina dzēst kravu ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var cargo = await _context.Cargo.Include(c => c.Devices).FirstOrDefaultAsync(c=> c.CargoId == cargoId);

            if (cargo == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoService] Krava ar ID {CargoId} nav atrasta dzēšanai.", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false; // Krava nav atrasta
            }

            // Piemērs biznesa loģikai: neļaut dzēst kravu, ja tā ir "InTransit"
            if (cargo.Status == CargoStatus.InTransit)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[CargoService] Nevar dzēst kravu ar ID {CargoId}, jo tās statuss ir 'InTransit'.", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException("Nevar dzēst kravu, kas ir ceļā (InTransit).");
            }

            // Atsaistam visas piesaistītās ierīces
            if (cargo.Devices != null)
            {
                foreach (var device in cargo.Devices)
                {
                    device.CargoId = null;
                    _context.Entry(device).State = EntityState.Modified;
                }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogInformation("[CargoService] Atsaistītas ierīces no kravas {CargoId} pirms dzēšanas.", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            }

            _context.Cargo.Remove(cargo);

            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[CargoService] Krava ar ID {CargoId} veiksmīgi dzēsta.", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[CargoService] Kļūda, dzēšot kravu ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Palīgmetode, lai konvertētu Cargo entītiju uz CargoViewModel.
        // Šo varētu realizēt arī ar AutoMapper lielākos projektos.
        private CargoViewModel MapCargoToViewModel(Cargo cargo)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            return new CargoViewModel
            {
                CargoId = cargo.CargoId,
                Status = cargo.Status,
                SenderId = cargo.SenderId,
                SenderName = cargo.Sender?.Name, // Droša piekļuve ar ?. operatoru
                ClientId = cargo.ClientId,
                ClientName = cargo.Receiver?.Name,
                RouteId = cargo.RouteId,
                RouteDescription = cargo.Route != null ? $"{cargo.Route.StartPoint} → {cargo.Route.EndPoint}" : "N/A",
                CreatedAt = cargo.CreatedAt,
                UpdatedAt = (DateTime)cargo.UpdatedAt,
                Devices = cargo.Devices?.Select(d => new DeviceSimpleViewModel
                {
                    DeviceId = d.DeviceId,
                    Type = d.Type,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    LastUpdate = d.LastUpdate
                }).ToList() ?? new List<DeviceSimpleViewModel>()
            };
#pragma warning restore CS8629 // Nullable value type may be null.

        }
    }
}