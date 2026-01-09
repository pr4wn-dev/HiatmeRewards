using Newtonsoft.Json;

namespace HiatMeApp.Models;

public class DayOffRequest
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("user_id")]
    public int UserId { get; set; }
    
    [JsonProperty("request_date")]
    public string? RequestDate { get; set; }
    
    [JsonProperty("start_time")]
    public string? StartTime { get; set; }
    
    [JsonProperty("end_time")]
    public string? EndTime { get; set; }
    
    [JsonProperty("reason")]
    public string? Reason { get; set; }
    
    [JsonProperty("manager_reason")]
    public string? ManagerReason { get; set; }
    
    [JsonProperty("status")]
    public string? Status { get; set; }
    
    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }
    
    /// <summary>
    /// Gets a formatted display string for the request date
    /// </summary>
    public string DisplayDate
    {
        get
        {
            if (DateTime.TryParse(RequestDate, out var date))
            {
                return date.ToString("MMM dd, yyyy");
            }
            return RequestDate ?? "Unknown";
        }
    }
    
    /// <summary>
    /// Gets a formatted display string for the time range (if specified)
    /// </summary>
    public string DisplayTimeRange
    {
        get
        {
            if (string.IsNullOrEmpty(StartTime) && string.IsNullOrEmpty(EndTime))
            {
                return "Full Day";
            }
            
            var start = string.IsNullOrEmpty(StartTime) ? "" : FormatTime(StartTime);
            var end = string.IsNullOrEmpty(EndTime) ? "" : FormatTime(EndTime);
            
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                return $"{start} - {end}";
            }
            else if (!string.IsNullOrEmpty(start))
            {
                return $"From {start}";
            }
            else if (!string.IsNullOrEmpty(end))
            {
                return $"Until {end}";
            }
            
            return "Full Day";
        }
    }
    
    /// <summary>
    /// Gets the status with appropriate emoji/icon
    /// </summary>
    public string DisplayStatus
    {
        get
        {
            return Status?.ToLowerInvariant() switch
            {
                "approved" => "✅ Approved",
                "denied" => "❌ Denied",
                "pending" => "⏳ Pending",
                _ => Status ?? "Unknown"
            };
        }
    }
    
    /// <summary>
    /// Gets the color for the status badge
    /// </summary>
    public string StatusColor
    {
        get
        {
            return Status?.ToLowerInvariant() switch
            {
                "approved" => "#28a745",
                "denied" => "#dc3545",
                "pending" => "#ffc107",
                _ => "#6c757d"
            };
        }
    }
    
    private static string FormatTime(string time)
    {
        if (TimeSpan.TryParse(time, out var ts))
        {
            var dt = DateTime.Today.Add(ts);
            return dt.ToString("h:mm tt");
        }
        return time;
    }
}

