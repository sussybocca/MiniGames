using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public interface IGuideService
{
    string GetIntroduction();
    IEnumerable<GuideSection> GetSections();
}

public class GuideSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}