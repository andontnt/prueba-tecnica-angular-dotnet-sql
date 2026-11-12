using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

/// <summary>
/// Filtros opcionales del listado de bonos. La paginación la hereda de PaginationRequest.
/// </summary>
public class BonusFilter : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? StateCode { get; set; }
}
