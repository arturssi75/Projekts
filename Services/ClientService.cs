using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Data;
using Project.Models;
using Project.Models.DTOs; // ClientViewModel, ClientCreateDto, ClientUpdateDto
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Services
{
    // Serviss klientu (Client) datu pārvaldībai.
    public class ClientService
    {
        private readonly TransportContext _context;
        private readonly ILogger<ClientService> _logger;

        public ClientService(TransportContext context, ILogger<ClientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Iegūst visus klientus un konvertē tos uz ClientViewModel.
        public async Task<IEnumerable<ClientViewModel>> GetAllClientsAsync()
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientService] Iegūst visus klientus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var clients = await _context.Client
                                    .OrderBy(c => c.Name)
                                    .ToListAsync();
                return clients.Select(c => MapClientToViewModel(c)).ToList();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Kļūda, iegūstot visus klientus.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Iegūst konkrētu klientu pēc ID un konvertē uz ClientViewModel.
        public async Task<ClientViewModel?> GetClientByIdAsync(int clientId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientService] Iegūst klientu ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            try
            {
                var client = await _context.Client.FindAsync(clientId);
                return client == null ? null : MapClientToViewModel(client);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Kļūda, iegūstot klientu ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Izveido jaunu klientu no ClientCreateDto.
        // Šo metodi parasti izsauktu admins vai dispečers. Ja klients reģistrējas pats,
        // Client ieraksts tiek veidots AccountController/Identity procesā.
        public async Task<ClientViewModel> CreateClientAsync(ClientCreateDto clientDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientService] Veido jaunu klientu ar nosaukumu: {ClientName}", clientDto.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (clientDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientService] CreateClientAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(clientDto));
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            var client = new Client
            {
                Name = clientDto.Name
                // Šeit varētu būt loģika, ja Client tiek sasaistīts ar ApplicationUser manuāli
            };
#pragma warning restore CS8601 // Possible null reference assignment.

            _context.Client.Add(client);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientService] Klients ar ID {ClientId} veiksmīgi izveidots.", client.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapClientToViewModel(client);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Kļūda, veidojot jaunu klientu: {ClientName}", clientDto.Name);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Atjaunina esoša klienta datus no ClientUpdateDto.
        public async Task<ClientViewModel?> UpdateClientAsync(ClientUpdateDto clientDto)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientService] Atjaunina klientu ar ID: {ClientId}", clientDto.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            if (clientDto == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                 _logger.LogWarning("[ClientService] UpdateClientAsync saņēma null DTO.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new ArgumentNullException(nameof(clientDto));
            }

            var existingClient = await _context.Client.FindAsync(clientDto.ClientId);
            if (existingClient == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientService] Klients ar ID {ClientId} nav atrasts atjaunināšanai.", clientDto.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return null; // Klients nav atrasts
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            existingClient.Name = clientDto.Name;
#pragma warning restore CS8601 // Possible null reference assignment.
            // Pievienot citas atjaunināmas īpašības, ja tādas ir

            _context.Entry(existingClient).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientService] Klients ar ID {ClientId} veiksmīgi atjaunināts.", existingClient.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return MapClientToViewModel(existingClient);
            }
            catch (DbUpdateConcurrencyException ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Konkurences kļūda, atjauninot klientu ar ID: {ClientId}", clientDto.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw; // Pārmet tālāk kontrolierim
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Kļūda, atjauninot klientu ar ID: {ClientId}", clientDto.ClientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Dzēš klientu pēc ID.
        // Pārbauda, vai klientu drīkst dzēst (piem., nav aktīvu kravu).
        public async Task<bool> DeleteClientAsync(int clientId)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            _logger.LogInformation("[ClientService] Mēģina dzēst klientu ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var client = await _context.Client.FindAsync(clientId);

            if (client == null)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientService] Klients ar ID {ClientId} nav atrasts dzēšanai.", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return false; // Klients nav atrasts
            }

            // Pārbaude, vai klientam ir piesaistītas kravas
            bool hasCargos = await _context.Cargo.AnyAsync(c => c.ClientId == clientId);
            if (hasCargos)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogWarning("[ClientService] Nevar dzēst klientu ar ID {ClientId}, jo tam ir piesaistītas kravas.", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw new InvalidOperationException($"Nevar dzēst klientu '{client.Name}', jo tam ir piesaistītas kravas. Vispirms jādzēš vai jāpārasignē kravas.");
            }
            
            // Ja klients ir sasaistīts ar ApplicationUser, jāapsver arī šīs saites apstrāde.
            // Piemēram, atsaistīt ApplicationUser.ClientId vai dzēst arī ApplicationUser (atkarībā no loģikas).
            // Šeit vienkāršības labad pieņemam, ka tiek dzēsts tikai Client profils.
            // ApplicationUser.ClientId kļūs par null, pateicoties OnDelete(DeleteBehavior.SetNull) TransportContext.

            _context.Client.Remove(client);
            try
            {
                await _context.SaveChangesAsync();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogInformation("[ClientService] Klients ar ID {ClientId} veiksmīgi dzēsts.", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                return true;
            }
            catch (Exception ex)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                _logger.LogError(ex, "[ClientService] Kļūda, dzēšot klientu ar ID: {ClientId}", clientId);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                throw;
            }
        }

        // Palīgmetode Client konvertēšanai uz ClientViewModel
        private ClientViewModel MapClientToViewModel(Client client)
        {
            return new ClientViewModel
            {
                ClientId = client.ClientId,
                Name = client.Name
                // Šeit varētu ielādēt saistītā ApplicationUser datus, ja nepieciešams
                // Piemēram, ja Client modelī būtu ApplicationUserId:
                // var appUser = await _context.Users.FindAsync(client.ApplicationUserId);
                // AssociatedUserEmail = appUser?.Email;
            };
        }
    }
}