namespace FEMS.Domain.Enums;

/// <summary>Section 6 &amp; 20.2: Device / security events raised by the mobile app.</summary>
public enum DeviceEventType
{
    Login = 0,
    Logout = 1,
    FlightModeOn = 2,
    FlightModeOff = 3,
    PoweredOff = 4,
    UnregisteredDeviceAttempt = 5,
    AppInstalled = 6,
    AppUpdated = 7
}
