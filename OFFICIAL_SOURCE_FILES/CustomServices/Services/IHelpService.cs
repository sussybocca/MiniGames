using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public interface IHelpService
{
    IEnumerable<FaqItem> GetFAQs();
}

public class FaqItem
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}