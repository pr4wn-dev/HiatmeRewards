using Newtonsoft.Json;
using System.Collections.Generic;

namespace HiatMeApp.Models;

public class IncompleteMileageRecord
{
    [JsonProperty("mileage_id")]
    public int MileageId { get; set; }

    [JsonProperty("vehicle_id")]
    public int VehicleId { get; set; }

    [JsonProperty("user_id")]
    public int UserId { get; set; }

    [JsonProperty("missing_fields")]
    public List<string> MissingFields { get; set; } = new List<string>();
}