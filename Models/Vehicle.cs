using Newtonsoft.Json;

namespace HiatMeApp.Models
{
    public class Vehicle
    {
        [JsonProperty("vehicle_id")]
        public int VehicleId { get; set; }
        [JsonProperty("image_location")]
        public string? ImageLocation { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Vin { get; set; }
        public string? Color { get; set; }
        [JsonProperty("license_plate")]
        public string? LicensePlate { get; set; }
        public int Year { get; set; }
        [JsonProperty("current_user_id")]
        public int? CurrentUserId { get; set; }
        [JsonProperty("last_user_id")]
        public int? LastUserId { get; set; }
        [JsonProperty("date_assigned")]
        public string? DateAssigned { get; set; }
        [JsonProperty("date_last_used")]
        public string? DateLastUsed { get; set; }
        public float? Mileage { get; set; }
        [JsonProperty("mileage_record")]
        public MileageRecord? MileageRecord { get; set; }
        [JsonIgnore]
        public string? LastSixVin => Vin?.Length >= 6 ? Vin[^6..] : Vin;
    }

    public class MileageRecord
    {
        [JsonProperty("mileage_id")]
        public int MileageId { get; set; }
        [JsonProperty("vehicle_id")]
        public int VehicleId { get; set; }
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        [JsonProperty("start_miles")]
        public float? StartMiles { get; set; }
        [JsonProperty("start_miles_datetime")]
        public string? StartMilesDatetime { get; set; }
        [JsonProperty("ending_miles")]
        public float? EndingMiles { get; set; }
        [JsonProperty("ending_miles_datetime")]
        public string? EndingMilesDatetime { get; set; }
        [JsonProperty("created_at")]
        public string? CreatedAt { get; set; }
        [JsonProperty("missing_fields")]
        public List<string>? MissingFields { get; set; } // Added for AssignVehicleResponse compatibility
    }
}