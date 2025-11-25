using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HiatMeApp.Messages;

public sealed class RefreshVehiclePageMessage : ValueChangedMessage<string?>
{
    public RefreshVehiclePageMessage(string? reason) : base(reason)
    {
    }
}

