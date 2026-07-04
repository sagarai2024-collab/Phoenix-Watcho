namespace StudentLearningPortal.Models;

public class KnowledgeBase
{
    public int Id { get; set; }
    public string Topic { get; set; } = "";      // fee, course, eligibility, etc.
    public string Question { get; set; } = "";   // "What is the registration fee?"
    public string Answer { get; set; } = "";     // "The fee is ₹900 for all programs."
    public string Keywords { get; set; } = "";   // "fee cost price pay money charge"
}
