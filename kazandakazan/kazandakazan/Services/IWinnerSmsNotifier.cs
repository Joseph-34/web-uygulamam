using kazandakazan.Models;

namespace kazandakazan.Services;

public interface IWinnerSmsNotifier
{
    Task NotifyWinnerAsync(PotWinnerInfo winner, CancellationToken cancellationToken = default);
}
