namespace FEMS.Application.Common.Interfaces;

/// <summary>
/// Section 5.4 &amp; 20: Firebase Cloud Messaging push dispatch. FCM is a cloud service
/// independent of the on-prem hosting choice. Implementations should no-op (and log)
/// when Firebase credentials aren't configured, rather than failing the request that
/// triggered the notification.
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(string pushToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
}
