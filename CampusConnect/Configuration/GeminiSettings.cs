namespace CampusConnect.Configuration;

public class GeminiSettings
{
    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "gemini-1.5-flash";
    public int MaxTokens { get; set; } = 1500;
}