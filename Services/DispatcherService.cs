using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
using Project.Models;
using Project.Models.DTOs; // DispatcherViewModel, DispatcherCreateDto, DispatcherUpdateDto
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss dispečeru (Dispatcher/Sender) datu pārvaldībai.
    public class DispatcherService
    {
        private readonly TransportContext _context;
        private readonly ILogger<DispatcherService> _logger;

        public DispatcherService(TransportContext context, ILogger<DispatcherService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visus dispečerus un konvertē tos uz DispatcherViewModel.
        public async Task<IEnumerable<DispatcherViewModel>> GetAllDispatchersAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherService] Iegūst visus dispečerus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var dispatchers = await _context.Dispatcher
                                        .OrderBy(d => d.Name)
                                        .ToListAsync();
                return dispatchers.Select(d => MapDispatcherToViewModel(d)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Kļūda, iegūstot visus dispečerus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Iegūst konkrētu dispečeru pēc ID (SenderId) un konvertē uz DispatcherViewModel.
        public async Task<DispatcherViewModel?> GetDispatcherByIdAsync(int dispatcherId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherService] Iegūst dispečeru ar ID: {DispatcherId}", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                // Dispatcher modelī primārā atslēga ir SenderId
                var dispatcher = await _context.Dispatcher.FirstOrDefaultAsync(d => d.SenderId == dispatcherId);
                return dispatcher == null ? null : MapDispatcherToViewModel(dispatcher);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Kļūda, iegūstot dispečeru ar ID: {DispatcherId}", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Izveido jaunu dispečeru no DispatcherCreateDto.
        // Šo metodi parasti izsauc admins. Ja dispečers reģistrējas pats,
        // Dispatcher ieraksts tiek veidots AccountController/Identity procesā.
        public async Task<DispatcherViewModel> CreateDispatcherAsync(DispatcherCreateDto dispatcherDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherService] Veido jaunu dispečeru: {DispatcherName}", dispatcherDto.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (dispatcherDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherService] CreateDispatcherAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(dispatcherDto));
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            var dispatcher = new Dispatcher
            {
                Name = dispatcherDto.Name,
                Email = dispatcherDto.Email,
                Phone = dispatcherDto.Phone
            };
#pragma warning restore CS8601 // Possible null reference assignment.

            _context.Dispatcher.Add(dispatcher);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherService] Dispečers ar ID {SenderId} veiksmīgi izveidots.", dispatcher.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapDispatcherToViewModel(dispatcher);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Kļūda, veidojot jaunu dispečeru: {DispatcherName}", dispatcherDto.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Atjaunina esoša dispečera datus no DispatcherUpdateDto.
        public async Task<DispatcherViewModel?> UpdateDispatcherAsync(DispatcherUpdateDto dispatcherDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherService] Atjaunina dispečeru ar ID: {SenderId}", dispatcherDto.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (dispatcherDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherService] UpdateDispatcherAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(dispatcherDto));
            }
            
            var existingDispatcher = await _context.Dispatcher.FirstOrDefaultAsync(d => d.SenderId == dispatcherDto.SenderId);
            if (existingDispatcher == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherService] Dispečers ar ID {SenderId} nav atrasts atjaunināšanai.", dispatcherDto.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null; // Dispečers nav atrasts
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            existingDispatcher.Name = dispatcherDto.Name;
#pragma warning restore CS8601 // Possible null reference assignment.
            existingDispatcher.Email = dispatcherDto.Email;
            existingDispatcher.Phone = dispatcherDto.Phone;

            _context.Entry(existingDispatcher).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherService] Dispečers ar ID {SenderId} veiksmīgi atjaunināts.", existingDispatcher.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapDispatcherToViewModel(existingDispatcher);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Konkurences kļūda, atjauninot dispečeru ar ID: {SenderId}", dispatcherDto.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Kļūda, atjauninot dispečeru ar ID: {SenderId}", dispatcherDto.SenderId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Dzēš dispečeru pēc ID (SenderId).
        public async Task<bool> DeleteDispatcherAsync(int dispatcherId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[DispatcherService] Mēģina dzēst dispečeru ar ID: {DispatcherId}", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var dispatcher = await _context.Dispatcher.FirstOrDefaultAsync(d => d.SenderId == dispatcherId);

            if (dispatcher == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherService] Dispečers ar ID {DispatcherId} nav atrasts dzēšanai.", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false; // Dispečers nav atrasts
            }

            // Pārbaude, vai dispečeram ir piesaistītas kravas
            bool hasCargos = await _context.Cargo.AnyAsync(c => c.SenderId == dispatcherId);
            if (hasCargos)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[DispatcherService] Nevar dzēst dispečeru ar ID {DispatcherId}, jo tam ir piesaistītas kravas.", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Nevar dzēst dispečeru '{dispatcher.Name}', jo tas ir norādīts kā sūtītājs kravām. Vispirms dzēsiet vai pārasignējiet kravas.");
            }

            // Līdzīgi kā ClientService, jāapsver sasaiste ar ApplicationUser.
            // ApplicationUser.DispatcherId kļūs par null, pateicoties OnDelete(DeleteBehavior.SetNull) TransportContext.

            _context.Dispatcher.Remove(dispatcher);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[DispatcherService] Dispečers ar ID {DispatcherId} veiksmīgi dzēsts.", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[DispatcherService] Kļūda, dzēšot dispečeru ar ID: {DispatcherId}", dispatcherId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Palīgmetode Dispatcher konvertēšanai uz DispatcherViewModel
        private DispatcherViewModel MapDispatcherToViewModel(Dispatcher dispatcher)
        {
            return new DispatcherViewModel
            {
                SenderId = dispatcher.SenderId,
                Name = dispatcher.Name,
                Email = dispatcher.Email,
                Phone = dispatcher.Phone
            };
        }
    }
}