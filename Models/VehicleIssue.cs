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
}