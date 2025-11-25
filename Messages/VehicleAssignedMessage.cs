using CommunityToolkit.Mvvm.Messaging.Messages;
using HiatMeApp.Models;

namespace HiatMeApp.Messages;

public sealed class VehicleAssignedMessage : ValueChangedMessage<Vehicle>
{
    public VehicleAssignedMessage(Vehicle value) : base(value)
    {
    }
}

