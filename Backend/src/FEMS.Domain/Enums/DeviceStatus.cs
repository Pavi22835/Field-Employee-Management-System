namespace FEMS.Domain.Enums;

/// <summary>Section 6: Device status lifecycle.</summary>
public enum DeviceStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Revoked = 3,
    Lost = 4
}
