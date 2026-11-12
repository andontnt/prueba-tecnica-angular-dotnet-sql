using Microsoft.EntityFrameworkCore;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    public Task<Employee?> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber, cancellationToken);

    public Task<bool> BonusTypeExistsAsync(int bonusTypeId, CancellationToken cancellationToken) =>
        context.BonusTypes.AnyAsync(x => x.BonusTypeId == bonusTypeId, cancellationToken);

    public Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken) =>
        context.BonusReasons.AnyAsync(x => x.ReasonCode == reasonCode && x.State, cancellationToken);

    public Task<bool> HasActiveBonusAsync(string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken) =>
        context.Bonuses.AnyAsync(x =>
            x.DepartmentCode == departmentCode
            && x.EmployeeNumber == employeeNumber
            && x.Period == period
            && x.DeletedAt == null
            && x.BonusStateCode != BonusStates.Cancelled, cancellationToken);

    public Task<Bonus?> GetByIdAsync(int bonusId, CancellationToken cancellationToken) =>
        context.Bonuses.FirstOrDefaultAsync(x => x.BonusId == bonusId, cancellationToken);

    public Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilter filter, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public void Add(Bonus bonus) => context.Bonuses.Add(bonus);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
