using BonusTest.Domain.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Entities;

public class Bonus
{
    public int BonusId { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public int EmployeeNumber { get; set; }
    public string Period { get; set; } = null!;
    public int BonusTypeId { get; set; }
    public decimal BonusValue { get; set; }
    public string ReasonCode { get; set; } = null!;
    public decimal MonthlySalary { get; set; }
    public decimal TaxRate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string BonusStateCode { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public BonusType BonusType { get; set; } = null!;
    public BonusReason Reason { get; set; } = null!;
    public State State { get; set; } = null!;
    public List<BonusProjectDistribution> ProjectDistributions { get; set; } = [];

    /// <summary>
    /// Calcula bruto, retención y neto con el sueldo y la retención guardados en el bono.
    /// </summary>
    public void CalculateAmounts()
    {
        GrossAmount = Money.Round(BonusTypeId == BonusTypes.FixedAmount
            ? BonusValue
            : MonthlySalary * BonusValue / 100);
        TaxAmount = Money.Round(GrossAmount * TaxRate / 100);
        NetAmount = GrossAmount - TaxAmount;
    }

    /// <summary>
    /// Reparte el bruto entre los proyectos en proporción a las horas y pasa el bono a Pagado.
    /// Cada línea se redondea a 2 decimales; el último proyecto (por Id) recibe lo que falta
    /// para cuadrar exacto, y si hay un solo proyecto recibe todo.
    /// </summary>
    public void Pay(IReadOnlyList<EmployeeProjectHours> projects, string userCode, DateTime now)
    {
        var orderedProjects = projects.OrderBy(x => x.EmployeeProjectHoursId).ToList();
        var totalHours = orderedProjects.Sum(x => x.Hours);

        if (orderedProjects.Count == 0 || totalHours <= 0)
            throw new BusinessException("BONUS_NO_PROJECTS", "El empleado no tiene horas registradas en proyectos.");

        var distributed = 0m;
        for (var i = 0; i < orderedProjects.Count; i++)
        {
            var project = orderedProjects[i];
            var amount = i == orderedProjects.Count - 1
                ? GrossAmount - distributed
                : Money.Round(GrossAmount * project.Hours / totalHours);
            distributed += amount;

            ProjectDistributions.Add(new BonusProjectDistribution
            {
                EmployeeProjectHoursId = project.EmployeeProjectHoursId,
                ProjectName = project.ProjectName,
                Hours = project.Hours,
                Amount = amount,
                CreatedBy = userCode,
                CreatedAt = now
            });
        }

        BonusStateCode = BonusStates.Paid;
        ModifiedBy = userCode;
        ModifiedAt = now;
    }
}
