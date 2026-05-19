using Microsoft.EntityFrameworkCore;
using Sferity.Backend.Data;
using Sferity.Backend.DTOs;
using Sferity.Backend.Models;
using TimeZoneConverter;

namespace Sferity.Backend.Services
{
    public class PromoCodeExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PromoCodeExpiryService> _logger;
        private readonly IPolandTimeService _time;

        public PromoCodeExpiryService(IServiceScopeFactory scopeFactory, ILogger<PromoCodeExpiryService> logger, IPolandTimeService time)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _time = time;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = _time.DelayUntilMidnight();
                _logger.LogInformation("Next promo code expiry check in {Minutes} minutes.", (int)delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);
                // await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                await RunExpiryCheckAsync(stoppingToken);
            }
        }

        public async Task RunExpiryCheckAsync(CancellationToken stoppingToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPromoCodeService>();

                var activatedCount = await service.ActivatePendingCodesAsync();
                var expiredCount = await service.ExpirePromoCodesAsync();
                   
                _logger.LogInformation("Promo code check complete at {Time}: {Activated} activated, {Expired} expired.",
                    DateTime.UtcNow, activatedCount, expiredCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during promo code check.");
            }
        }
    }
}