using FEMS.Application.Common.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Section 20: FCM push dispatch via the Firebase Admin SDK. Reads the service account
/// JSON path from `Fcm:ServiceAccountKeyPath` (appsettings / IIS environment variable).
/// If it's not configured, sends are logged and skipped rather than throwing — a missing
/// push credential should never block the request that triggered the notification
/// (e.g. creating a field assignment).
/// </summary>
public class FcmPushNotificationService : IPushNotificationService
{
    private static FirebaseApp? _app;
    private static readonly object InitLock = new();

    private readonly ILogger<FcmPushNotificationService> _logger;
    private readonly bool _configured;

    public FcmPushNotificationService(IConfiguration configuration, ILogger<FcmPushNotificationService> logger)
    {
        _logger = logger;

        var keyPath = configuration["Fcm:ServiceAccountKeyPath"];
        _configured = !string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath);

        if (_configured && _app is null)
        {
            lock (InitLock)
            {
                _app ??= FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(keyPath)
                });
            }
        }
    }

    public async Task SendAsync(string pushToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (!_configured || string.IsNullOrWhiteSpace(pushToken))
        {
            _logger.LogInformation("FCM not configured or no push token supplied; skipping push send: {Title}", title);
            return;
        }

        try
        {
            var message = new Message
            {
                Token = pushToken,
                Notification = new Notification { Title = title, Body = body },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            // A failed push should never fail the API request that triggered it.
            _logger.LogWarning(ex, "FCM push send failed for token ending in {TokenSuffix}", pushToken[^Math.Min(6, pushToken.Length)..]);
        }
    }
}
