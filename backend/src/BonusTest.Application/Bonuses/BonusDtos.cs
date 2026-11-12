namespace BonusTest.Application.Bonuses;

public record EmployeeDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate);

public record CreateBonusRequest(
    string DepartmentCode,
    int EmployeeNumber,
    string Period,
    int BonusTypeId,
    decimal BonusValue,
    string ReasonCode);

public record BonusListItemDto(
    int BonusId,
    string BonusStateCode,
    string StateDescription,
    string Period,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeName,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal BonusValue,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount);
