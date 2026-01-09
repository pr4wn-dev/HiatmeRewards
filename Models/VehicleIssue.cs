using Microsoft.Maui.Graphics;
using Newtonsoft.Json;

namespace HiatMeApp.Models;

public class VehicleIssue
{
    [JsonProperty("issue_id")]
    public int IssueId { get; set; }

    [JsonProperty("issue_type")]
    public string? IssueType { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("resolved_at")]
    public string? ResolvedAt { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("resolution_id")]
    public int? ResolutionId { get; set; }

    [JsonProperty("parts_replaced")]
    public string? PartsReplaced { get; set; }

    [JsonProperty("work_done")]
    public string? WorkDone { get; set; }

    [JsonProperty("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [JsonProperty("labor_hours")]
    public float? LaborHours { get; set; }

    [JsonProperty("repair_cost")]
    public float? RepairCost { get; set; }

    [JsonProperty("mechanic_notes")]
    public string? MechanicNotes { get; set; }

    [JsonProperty("repair_category")]
    public string? RepairCategory { get; set; }

    [JsonProperty("reported_by")]
    public int? ReportedBy { get; set; }

    [JsonProperty("reporter_name")]
    public string? ReporterName { get; set; }

    // Display helper for reporter name
    public string DisplayReporter => !string.IsNullOrEmpty(ReporterName) ? ReporterName : "Unknown";

    // Display helper for status badge color
    public Color StatusColor => Status?.ToLower() switch
    {
        "resolved" => Color.FromArgb("#28a745"),   // Green
        "in progress" or "in_progress" => Color.FromArgb("#ffc107"), // Yellow
        "pending" => Color.FromArgb("#17a2b8"),    // Blue
        _ => Color.FromArgb("#dc3545")              // Red for open/unknown
    };
}