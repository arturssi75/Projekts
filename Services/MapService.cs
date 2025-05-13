using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
using Project.Models;
using Project.Models.DTOs; // MapDeviceViewModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss kartes datu apstrādei.
    public class MapService
    {
        private readonly TransportContext _context;
        private readonly ILogger<MapService> _logger;

        // Iepriekš konstruktoram nebija ILogger, pievienojam to.
        public MapService(TransportContext context, ILogger<MapService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visas aktīvās ierīces ar derīgām koordinātēm kartes attēlošanai.
        // "Aktīvas" definīcija varētu būt balstīta uz LastUpdate laiku vai citiem kritērijiem.
        // Šeit vienkārši atlasām visas ar koordinātēm.
        public async Task<IEnumerable<MapDeviceViewModel>> GetActiveDevicesWithCoordinatesAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[MapService] Iegūst ierīces kartei.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var devices = await _context.Device
                    .Where(d => d.Latitude != 0 || d.Longitude != 0) // Iekļauj ierīces ar vismaz vienu nenulles koordināti
                    .OrderByDescending(d => d.LastUpdate)
                    .Select(d => new MapDeviceViewModel // Tieša projekcija uz DTO vaicājumā
                    {
                        DeviceId = d.DeviceId,
                        Type = d.Type.ToString(), // Konvertē enum uz string
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        LastUpdate = d.LastUpdate,
                        CargoId = d.CargoId
                    })
                    .ToListAsync();
                return devices;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[MapService] Kļūda, iegūstot ierīces kartei.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }
        
        // Atjaunina konkrētas ierīces atrašanās vietu un pēdējās atjaunināšanas laiku.
        // Šo metodi varētu izsaukt, piemēram, no fona servisa, kas saņem datus no IoT ierīcēm.
        public async Task<bool> UpdateDeviceLocationAsync(int deviceId, decimal latitude, decimal longitude)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[MapService] Atjaunina atrašanās vietu ierīcei ar ID: {DeviceId}", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var device = await _context.Device.FindAsync(deviceId);
            if (device != null)
            {
                device.Latitude = latitude;
                device.Longitude = longitude;
                device.LastUpdate = DateTime.UtcNow; // Vienmēr atjaunina laiku
                try
                {
                    await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    _logger.LogInformation("[MapService] Atrašanās vieta ierīcei {DeviceId} veiksmīgi atjaunināta.", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    return true;
                }
                catch (Exception ex)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                     _logger.LogError(ex, "[MapService] Kļūda, atjauninot atrašanās vietu ierīcei {DeviceId}.", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                    throw;
                }
            }
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogWarning("[MapService] Ierīce ar ID {DeviceId} nav atrasta atrašanās vietas atjaunināšanai.", deviceId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            return false;
        }

        // Iegūst ierīces, kas piesaistītas konkrētai kravai (ja tāda ir viena primārā ierīce).
        // Ja kravai var būt vairākas ierīces, šī metode jāpārveido.
        // Pašreizējais modelis (Device.CargoId) pieļauj tikai vienu kravu ierīcei.
        public async Task<MapDeviceViewModel?> GetDeviceByCargoIdAsync(int cargoId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[MapService] Iegūst ierīci kravai ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var device = await _context.Device
                    .Where(d => d.CargoId == cargoId)
                    .Select(d => new MapDeviceViewModel
                    {
                        DeviceId = d.DeviceId,
                        Type = d.Type.ToString(),
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        LastUpdate = d.LastUpdate,
                        CargoId = d.CargoId
                    })
                    .FirstOrDefaultAsync();
                return device;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogError(ex, "[MapService] Kļūda, iegūstot ierīci kravai ar ID: {CargoId}", cargoId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }
    }
}