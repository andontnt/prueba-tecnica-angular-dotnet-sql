using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<Employee?> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task<bool> BonusTypeExistsAsync(int bonusTypeId, CancellationToken cancellationToken);
    Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken);
    Task<bool> HasActiveBonusAsync(string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task<Bonus?> GetByIdAsync(int bonusId, CancellationToken cancellationToken);
    Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilter filter, CancellationToken cancellationToken);
    void Add(Bonus bonus);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
