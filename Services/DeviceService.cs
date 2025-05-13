using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
using Project.Models;
using Project.Models.DTOs; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    public class DeviceService
    {
        private readonly TransportContext _context;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(TransportContext context, ILogger<DeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<DeviceViewModel>> GetAllDevicesAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Iegūst visas ierīces.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                // Izmaiņa: .Select(d => MapDeviceToViewModel(d)) tiek izsaukts pēc ToListAsync,
                // vai arī MapDeviceToViewModel tiek padarīta par statisku.
                // Šeit izvēlamies padarīt MapDeviceToViewModel par statisku,
                // jo tā neizmanto instances mainīgos _context vai _logger.
                var devices = await _context.Device
                                    .OrderBy(d => d.DeviceId)
                                    .Select(d => MapDeviceToViewModel(d)) // EF Core varētu mēģināt to tulkot, ja metode ir vienkārša
                                    .ToListAsync(); // Ja Select ar instances metodi neizdodas, tad vispirms ToListAsync()
                
                // Alternatīva, ja MapDeviceToViewModel nevar būt statiska vai EF Core to netulko:
                // var devicesFromDb = await _context.Device
                //                             .OrderBy(d => d.DeviceId)
                //                             .ToListAsync();
                // return devicesFromDb.Select(d => MapDeviceToViewModel(d)).ToList();
                
                return devices; // Ja .Select(MapDeviceToViewModel) strādā pirms ToListAsync ar statisku metodi
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, iegūstot visas ierīces.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        public async Task<DeviceViewModel?> GetDeviceByIdAsync(int deviceId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Iegūst ierīci ar ID: {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var device = await _context.Device.FindAsync(deviceId);
                // Izmantojam statisko metodi arī šeit
                return device == null ? null : MapDeviceToViewModel(device);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, iegūstot ierīci ar ID: {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        public async Task<DeviceViewModel> CreateDeviceAsync(DeviceCreateDto deviceDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Veido jaunu ierīci ar tipu: {DeviceType}", deviceDto.Type);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
             if (deviceDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceService] CreateDeviceAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(deviceDto));
            }

            var device = new Device
            {
                Type = deviceDto.Type,
                Latitude = deviceDto.Latitude,
                Longitude = deviceDto.Longitude,
                LastUpdate = DateTime.UtcNow 
            };
            _context.Device.Add(device);
            
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceService] Ierīce (bez vēstures) ar ID {DeviceId} sākotnēji saglabāta.", device.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

                if (device.DeviceId == 0)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogError("[DeviceService] Kļūda: DeviceId joprojām ir 0 pēc pirmās SaveChangesAsync.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    throw new InvalidOperationException("Neizdevās iegūt DeviceId no datu bāzes.");
                }

                var historyEntry = new DeviceHistory
                {
                    DeviceId = device.DeviceId, 
                    Latitude = device.Latitude,
                    Longitude = device.Longitude,
                    Timestamp = device.LastUpdate ?? DateTime.UtcNow
                };
                _context.DeviceHistory.Add(historyEntry);
                await _context.SaveChangesAsync(); 

#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceService] Ierīce ar ID {DeviceId} un tās pirmais vēstures ieraksts veiksmīgi izveidoti un saglabāti.", device.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                // Izmantojam statisko metodi
                return MapDeviceToViewModel(device);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, veidojot jaunu ierīci (vai tās vēsturi) ar tipu: {DeviceType}", deviceDto.Type);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        public async Task<DeviceViewModel?> UpdateDeviceAsync(DeviceUpdateDto deviceDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Atjaunina ierīci ar ID: {DeviceId}", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (deviceDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceService] UpdateDeviceAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(deviceDto));
            }

            var existingDevice = await _context.Device.FindAsync(deviceDto.DeviceId);
            if (existingDevice == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceService] Ierīce ar ID {DeviceId} nav atrasta atjaunināšanai.", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null; 
            }

            bool coordinatesChanged = existingDevice.Latitude != deviceDto.Latitude || existingDevice.Longitude != deviceDto.Longitude;

            existingDevice.Type = deviceDto.Type;
            existingDevice.Latitude = deviceDto.Latitude;
            existingDevice.Longitude = deviceDto.Longitude;
            existingDevice.LastUpdate = DateTime.UtcNow; 
            
            if (existingDevice.CargoId != deviceDto.CargoId)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogInformation("[DeviceService] Maina CargoId ierīcei {DeviceId} no {OldCargoId} uz {NewCargoId}",
                    existingDevice.DeviceId, existingDevice.CargoId, deviceDto.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                existingDevice.CargoId = deviceDto.CargoId;
            }

            if (coordinatesChanged)
            {
                var historyEntry = new DeviceHistory
                {
                    DeviceId = existingDevice.DeviceId,
                    Latitude = existingDevice.Latitude,
                    Longitude = existingDevice.Longitude,
                    Timestamp = existingDevice.LastUpdate ?? DateTime.UtcNow
                };
                _context.DeviceHistory.Add(historyEntry);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceService] Pievienots jauns vēstures ieraksts ierīcei ID {DeviceId} koordinātu maiņas dēļ.", existingDevice.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            }
            
            _context.Entry(existingDevice).State = EntityState.Modified; 
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceService] Ierīce ar ID {DeviceId} veiksmīgi atjaunināta.", existingDevice.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                // Izmantojam statisko metodi
                return MapDeviceToViewModel(existingDevice);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Konkurences kļūda, atjauninot ierīci ar ID: {DeviceId}", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogWarning("Ieraksts tika dzēsts cita lietotāja darbības rezultātā ierīcei ID: {DeviceId}", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                     _logger.LogWarning("Ieraksts tika modificēts cita lietotāja darbības rezultātā ierīcei ID: {DeviceId}", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                throw; 
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, atjauninot ierīci ar ID: {DeviceId}", deviceDto.DeviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        public async Task<bool> DeleteDeviceAsync(int deviceId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Mēģina dzēst ierīci ar ID: {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var device = await _context.Device.FindAsync(deviceId);

            if (device == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceService] Ierīce ar ID {DeviceId} nav atrasta dzēšanai.", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false; 
            }

            if (device.CargoId.HasValue)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DeviceService] Nevar dzēst ierīci ar ID {DeviceId}, jo tā ir piesaistīta kravai {CargoId}.", deviceId, device.CargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Nevar dzēst ierīci (ID: {deviceId}), jo tā ir piesaistīta kravai (ID: {device.CargoId}). Vispirms atsaistiet ierīci no kravas.");
            }
            
            _context.Device.Remove(device); 
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DeviceService] Ierīce ar ID {DeviceId} veiksmīgi dzēsta.", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, dzēšot ierīci ar ID: {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        public async Task<IEnumerable<DeviceHistoryPointDto>> GetDeviceHistoryAsync(int deviceId, DateTime? startDate, DateTime? endDate)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DeviceService] Iegūst vēsturi ierīcei {DeviceId} no {StartDate} līdz {EndDate}", deviceId, startDate, endDate);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            
            var query = _context.DeviceHistory
                                .AsNoTracking() 
                                .Where(h => h.DeviceId == deviceId);

            if (startDate.HasValue)
            {
                query = query.Where(h => h.Timestamp >= startDate.Value.Date); 
            }
            if (endDate.HasValue)
            {
                var inclusiveEndDate = endDate.Value.Date.AddDays(1); 
                query = query.Where(h => h.Timestamp < inclusiveEndDate);
            }

            try
            {
                return await query.OrderBy(h => h.Timestamp)
                                  .Select(h => new DeviceHistoryPointDto // Tieša projekcija uz DTO vaicājumā ir droša
                                  {
                                      Latitude = h.Latitude,
                                      Longitude = h.Longitude,
                                      Timestamp = h.Timestamp
                                  })
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DeviceService] Kļūda, iegūstot vēsturi ierīcei {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Pārvēršam par STATISKU metodi, lai izvairītos no EF Core klienta puses novērtēšanas problēmām
        private static DeviceViewModel MapDeviceToViewModel(Device device)
        {
            if (device == null)
            {
                // Lai gan šis gadījums netiks sasniegts, ja izsauc no GetAllDevicesAsync pēc ToListAsync,
                // vai ja FindAsync atgriež null un mēs to pārbaudām, laba prakse ir pārbaudīt.
                // Šeit mēs varētu mest ArgumentNullException vai atgriezt noklusējuma/null vērtību,
                // bet, tā kā metode ir privāta un tiek izsaukta kontrolētos apstākļos,
                // pieņemsim, ka 'device' nekad nebūs null šeit, ja loģika pirms tam ir korekta.
                // Ja tomēr tas notiek, NullReferenceException būs skaidrāka nekā kļūda vēlāk.
                // Tomēr, labāk ir apstrādāt šo gadījumu eleganti.
                // Tā kā metode tagad ir statiska, tai nav piekļuves _logger.
                Console.WriteLine("[DeviceService] MapDeviceToViewModel saņēma null Device objektu. Tas nedrīkstētu notikt, ja iepriekšējā loģika ir pareiza.");
                // Atgriežam tukšu objektu vai metam izņēmumu, atkarībā no vēlamās uzvedības.
                // Šajā gadījumā, ja tas notiek, tas ir nopietna kļūda, tāpēc izņēmums ir pamatots.
                throw new ArgumentNullException(nameof(device), "Device objekts nevar būt null priekš mapēšanas statiskā metodē.");
            }
            return new DeviceViewModel
            {
                DeviceId = device.DeviceId,
                Type = device.Type,
                Latitude = device.Latitude,
                Longitude = device.Longitude,
                LastUpdate = device.LastUpdate,
                CargoId = device.CargoId
            };
        }
    }
}
