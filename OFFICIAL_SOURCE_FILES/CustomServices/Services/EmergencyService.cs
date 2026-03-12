using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public class EmergencyService : IEmergencyService
{
    public IEnumerable<EmergencyContact> GetContacts()
    {
        // In a real app, these could come from configuration or a database
        return new List<EmergencyContact>
        {
            new()
            {
                Title = "Technical Support",
                Description = "24/7 assistance for system crashes, game loading failures, and critical bugs.",
                Phone = "+1-800-555-0199",
                Email = "techsupport@minigames.example",
                Url = "https://support.minigames.example",
                LinkText = "Knowledge Base",
                ChatUrl = "https://chat.minigames.example"
            },
            new()
            {
                Title = "Security & Abuse",
                Description = "Report security vulnerabilities, phishing attempts, or abusive content.",
                Email = "security@minigames.example",
                Url = "https://security.minigames.example",
                LinkText = "Responsible Disclosure Policy"
            },
            new()
            {
                Title = "Community Manager",
                Description = "For urgent community-related issues (harassment, moderation appeals).",
                Email = "community@minigames.example",
                ChatUrl = "https://discord.gg/minigames"
            }
        };
    }
}