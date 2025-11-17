namespace ExamManagement.Models.Rubric;

public class Rubric
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<RubricItem> Items { get; set; } = new List<RubricItem>();
}

public class RubricItem
{
    public int Id { get; set; }
    public int RubricId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Rubric Rubric { get; set; } = null!;
}

