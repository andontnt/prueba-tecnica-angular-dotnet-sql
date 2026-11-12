using BonusTest.Application.Common;

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

/// <summary>Filtros opcionales del listado + paginación (PageIndex, PageSize).</summary>
public class BonusFilter : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? StateCode { get; set; }
}
