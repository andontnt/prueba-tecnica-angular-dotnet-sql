using BonusTest.Domain.Common;
using BonusTest.Domain.Constants;

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
}
