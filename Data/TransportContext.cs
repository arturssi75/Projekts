using Microsoft.EntityFrameworkCore;
using Project.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; 
using DbRoute = Project.Models.Route; // Alias saglabāts

namespace Project.Data
{
    public class TransportContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public TransportContext(DbContextOptions<TransportContext> options)
            : base(options)
        {
        }

        public DbSet<Cargo> Cargo { get; set; } = default!;
        public DbSet<Client> Client { get; set; } = default!;
        public DbSet<Device> Device { get; set; } = default!;
        public DbSet<Dispatcher> Dispatcher { get; set; } = default!;
        public DbSet<DbRoute> Route { get; set; } = default!;
        public DbSet<Vehicle> Vehicle { get; set; } = default!;
        
        // JAUNS: DbSet priekš ierīču vēstures
        public DbSet<DeviceHistory> DeviceHistory { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            // === Optimistic Concurrency konfigurācija ===
            modelBuilder.Entity<Cargo>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<Client>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<Device>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<Dispatcher>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<DbRoute>().Property(p => p.RowVersion).IsRowVersion(); // Izmantojam alias DbRoute
            modelBuilder.Entity<Vehicle>().Property(p => p.RowVersion).IsRowVersion();
            // Piezīme: ApplicationUser, ApplicationRole, DeliveryReport, Notification, DeviceHistory
            // parasti nav nepieciešama tikpat stingra RowVersion kontrole kā galvenajām datu entītijām,
            // bet to var pievienot, ja jūsu biznesa loģika to prasa.

            // === Attiecību konfigurācija (esošā un jaunā) ===
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.Client)
                .WithMany() 
                .HasForeignKey(au => au.ClientId) 
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.Dispatcher)
                .WithMany() 
                .HasForeignKey(au => au.DispatcherId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cargo>()
                .HasOne(c => c.Receiver)
                .WithMany(cl => cl.ReceivedCargos) 
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cargo>()
                .HasOne(c => c.Sender)
                .WithMany(d => d.SentCargos) 
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cargo>()
                .HasOne(c => c.Route)
                .WithMany(r => r.Cargos) 
                .HasForeignKey(c => c.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Device>()
                .HasOne(d => d.Cargo)
                .WithMany(c => c.Devices) 
                .HasForeignKey(d => d.CargoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            if (typeof(Cargo).GetProperty("VehicleId") != null) 
            {
                modelBuilder.Entity<Cargo>()
                    .HasOne(c => c.Vehicle)
                    .WithMany() 
                    .HasForeignKey(c => c.VehicleId)
                    .IsRequired(false) 
                    .OnDelete(DeleteBehavior.SetNull); 
            }

            modelBuilder.Entity<Device>()
                .Property(d => d.Latitude)
                .HasColumnType("decimal(9,6)");

            modelBuilder.Entity<Device>()
                .Property(d => d.Longitude)
                .HasColumnType("decimal(9,6)");

            // JAUNS: Konfigurācija DeviceHistory entītijai
            modelBuilder.Entity<DeviceHistory>()
                .Property(dh => dh.Latitude)
                .HasColumnType("decimal(9,6)");

            modelBuilder.Entity<DeviceHistory>()
                .Property(dh => dh.Longitude)
                .HasColumnType("decimal(9,6)");
            
            modelBuilder.Entity<DeviceHistory>()
                .HasOne(dh => dh.Device)
                .WithMany() // Ja Device modelī nav ICollection<DeviceHistory>
                .HasForeignKey(dh => dh.DeviceId)
                .OnDelete(DeleteBehavior.Cascade); // Dzēšot ierīci, dzēš saistīto vēsturi
        }
    }
}
