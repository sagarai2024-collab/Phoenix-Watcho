namespace StudentLearningPortal.Models;

public class ChatLog
{
    public int Id { get; set; }
    public string UserMessage { get; set; } = "";
    public string BotReply { get; set; } = "";
    public string Topic { get; set; } = "general";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
