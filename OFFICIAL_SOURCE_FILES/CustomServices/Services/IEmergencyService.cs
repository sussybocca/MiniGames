using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public interface IEmergencyService
{
    IEnumerable<EmergencyContact> GetContacts();
}

public class EmergencyContact
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Url { get; set; }
    public string? LinkText { get; set; }
    public string? ChatUrl { get; set; }
}