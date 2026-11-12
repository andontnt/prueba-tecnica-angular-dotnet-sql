namespace BonusTest.Domain.Entities;

public class BonusProjectDistribution
{
    public int BonusProjectDistributionId { get; set; }
    public int BonusId { get; set; }
    public int EmployeeProjectHoursId { get; set; }
    public string ProjectName { get; set; } = null!;
    public decimal Hours { get; set; }
    public decimal Amount { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
