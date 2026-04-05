namespace CampusConnect.Configuration;

public class OpenAISettings
{
    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 1500;
}