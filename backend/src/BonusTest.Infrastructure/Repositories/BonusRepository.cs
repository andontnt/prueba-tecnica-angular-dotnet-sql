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

    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilter filter, CancellationToken cancellationToken)
    {
        var query = context.Bonuses.AsNoTracking().Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            query = query.Where(x => x.DepartmentCode == filter.DepartmentCode);

        if (filter.EmployeeNumber is not null)
            query = query.Where(x => x.EmployeeNumber == filter.EmployeeNumber);

        if (!string.IsNullOrWhiteSpace(filter.Period))
            query = query.Where(x => x.Period == filter.Period);

        if (!string.IsNullOrWhiteSpace(filter.StateCode))
            query = query.Where(x => x.BonusStateCode == filter.StateCode);

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(x => x.BonusId)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new BonusListItemDto(
                x.BonusId,
                x.BonusStateCode,
                x.State.StateDescription,
                x.Period,
                x.DepartmentCode,
                x.Employee.Department.DepartmentName,
                x.EmployeeNumber,
                x.Employee.FullName,
                x.BonusTypeId,
                x.BonusType.BonusTypeDescription,
                x.BonusValue,
                x.Reason.ReasonDescription,
                x.GrossAmount,
                x.TaxAmount,
                x.NetAmount))
            .ToListAsync(cancellationToken);

        return PagedResponse<BonusListItemDto>.Create(data, filter.PageIndex, filter.PageSize, totalRecords);
    }

    public void Add(Bonus bonus) => context.Bonuses.Add(bonus);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
