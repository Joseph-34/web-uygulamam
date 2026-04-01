using System.Net.Http.Headers;
using System.Text;
using kazandakazan.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kazandakazan.Services;

/// <summary>
/// Twilio REST ile kazanan SMS’i. Twilio:AccountSid, AuthToken, FromPhone dolu değilse gönderim yapılmaz.
/// </summary>
public sealed class WinnerSmsNotifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WinnerSmsNotifier> logger) : IWinnerSmsNotifier
{
    public async Task NotifyWinnerAsync(PotWinnerInfo winner, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(winner.PhoneNumber))
        {
            logger.LogInformation("Kazanan {User} için kayıtlı telefon yok; SMS atlandı.", winner.UserName);
            return;
        }

        var sid = configuration["Twilio:AccountSid"];
        var token = configuration["Twilio:AuthToken"];
        var from = configuration["Twilio:FromPhone"];
        if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(from))
        {
            logger.LogInformation("Twilio yapılandırılmadı (Twilio:AccountSid / AuthToken / FromPhone); SMS atlandı.");
            return;
        }

        var to = NormalizePhone(winner.PhoneNumber);
        if (to is null)
        {
            logger.LogWarning("Geçersiz telefon formatı: {Phone}", winner.PhoneNumber);
            return;
        }

        var body =
            $"Tebrikler {winner.UserName}! Kazan Kazan simülasyonunda {winner.PrizeTry:N0} TL ödül kazandınız.";

        var client = httpClientFactory.CreateClient();
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json";
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{token}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = to,
            ["From"] = from,
            ["Body"] = body
        });

        try
        {
            var res = await client.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Twilio SMS başarısız {Status}: {Body}", (int)res.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twilio SMS gönderiminde hata.");
        }
    }

    private static string? NormalizePhone(string raw)
    {
        var t = raw.Trim();
        var d = new string(t.Where(char.IsDigit).ToArray());
        if (t.StartsWith('+') && d.Length >= 10)
            return "+" + d;
        if (d.Length == 10 && d.StartsWith('5'))
            return "+90" + d;
        if (d.Length == 11 && d.StartsWith("05"))
            return "+90" + d[1..];
        if (d.StartsWith("90") && d.Length >= 12)
            return "+" + d;
        if (d.Length >= 10)
            return "+" + d;
        return null;
    }
}
