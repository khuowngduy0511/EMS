namespace ExamManagement.Contracts.Rubric;

public class RubricDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public List<RubricItemDto> Items { get; set; } = new();
}

public class RubricItemDto
{
    public int Id { get; set; }
    public int RubricId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public int Order { get; set; }
}

public class CreateRubricRequest
{
    public int ExamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateRubricItemRequest> Items { get; set; } = new();
}

public class CreateRubricItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public int Order { get; set; }
}

