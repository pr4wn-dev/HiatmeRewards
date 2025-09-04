using System.Collections.Generic;

namespace HiatMeApp.Models;

public class User
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? Role { get; set; }
    public int UserId { get; set; } // Added to store user_id
    public List<Vehicle>? Vehicles { get; set; }
}