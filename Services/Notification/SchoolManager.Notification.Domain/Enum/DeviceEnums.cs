namespace SchoolManager.Notification.Domain
{
    /// <summary>Who owns a device token (from the login token's claims)</summary>
    public enum DeviceUserType
    {
        Parent = 1,
        Teacher = 2,
        Driver = 3,
        Employee = 4
    }

    /// <summary>The phone / browser a device token belongs to</summary>
    public enum DevicePlatform
    {
        Android = 1,
        iOS = 2,
        Web = 3
    }
}
