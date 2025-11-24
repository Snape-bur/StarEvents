using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.Enums;

public class BookingExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public BookingExpiryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Runs every 1 minute

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;

                var expiredBookings = await context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.Status == "Pending" &&
                                b.ReservationExpiresAt != null &&
                                b.ReservationExpiresAt < now)
                    .ToListAsync(stoppingToken);

                foreach (var b in expiredBookings)
                {
                    b.Status = "Expired";
                    b.PaymentStatus = PaymentStatus.Cancelled;

                    if (b.Event != null)
                        b.Event.AvailableSeats += b.Quantity;
                }

                if (expiredBookings.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
