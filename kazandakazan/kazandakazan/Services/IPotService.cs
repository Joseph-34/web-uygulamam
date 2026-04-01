using kazandakazan.Models.ViewModels;

namespace kazandakazan.Services;

public interface IPotService
{
    Task<PotStateDto?> GetPotStateAsync(Guid? userId, CancellationToken cancellationToken = default);

    Task<ContributeResult> ContributeAsync(Guid userId, int potId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Cüzdanda bırakılacak rezerv hariç, 5 ₺ katları kadarını tek işlemde aktarır.</summary>
    Task<ContributeResult> ContributeAllAsync(Guid userId, int potId, decimal reserveAmount, CancellationToken cancellationToken = default);

    /// <summary>Demo: simülasyon bakiyesi yükler (gerçek para değildir).</summary>
    Task<(bool ok, string message)> DemoAddWalletAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Banka yüklemesi arayüzü (simülasyon): 5 ₺ katları, gerçek havale/EFT yoktur.</summary>
    Task<(bool ok, string message)> SimulatedBankDepositAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);

    Task BroadcastStateAsync(CancellationToken cancellationToken = default);

    Task<LastWinnerInfo?> GetLastWinnerAsync(CancellationToken cancellationToken = default);
}
