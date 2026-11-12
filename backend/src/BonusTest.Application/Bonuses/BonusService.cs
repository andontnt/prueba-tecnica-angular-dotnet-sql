using System.Globalization;
using BonusTest.Application.Common;
using BonusTest.Domain.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository)
{
    public Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilter filter, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(filter, cancellationToken);

    public async Task<EmployeeDto> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await GetActiveEmployeeAsync(departmentCode, employeeNumber, cancellationToken);

        return new EmployeeDto(
            employee.DepartmentCode,
            employee.EmployeeNumber,
            employee.FullName,
            employee.HireDate,
            employee.MonthlySalary,
            employee.TaxRate);
    }

    public async Task<int> CreateAsync(CreateBonusRequest request, string userCode, CancellationToken cancellationToken)
    {
        var employee = await GetActiveEmployeeAsync(request.DepartmentCode, request.EmployeeNumber, cancellationToken);
        var now = DateTime.Now;

        // Periodo YYYY-MM, no posterior al mes actual.
        if (!DateTime.TryParseExact(request.Period, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var period))
            throw new BusinessException("PERIOD_INVALID", "El periodo debe tener el formato YYYY-MM.");

        if (period > new DateTime(now.Year, now.Month, 1))
            throw new BusinessException("PERIOD_IN_FUTURE", "El periodo no puede ser posterior al mes actual.");

        if (!await repository.BonusTypeExistsAsync(request.BonusTypeId, cancellationToken))
            throw new BusinessException("BONUS_TYPE_INVALID", "El tipo de bono no existe.");

        if (!await repository.IsReasonActiveAsync(request.ReasonCode, cancellationToken))
            throw new BusinessException("REASON_INACTIVE", "El motivo no existe o no está activo.");

        // Se redondea antes de validar: el valor guardado (DECIMAL(16,2)) es el mismo que se valida y se calcula.
        var bonusValue = Money.Round(request.BonusValue);

        // Monto fijo: > 0 y <= sueldo. Porcentaje: > 0 y <= 100.
        if (request.BonusTypeId == BonusTypes.FixedAmount)
        {
            if (bonusValue <= 0 || bonusValue > employee.MonthlySalary)
                throw new BusinessException("AMOUNT_INVALID", "El monto debe ser mayor a 0 y no mayor al sueldo mensual.");
        }
        else if (bonusValue <= 0 || bonusValue > 100)
        {
            throw new BusinessException("PERCENTAGE_INVALID", "El porcentaje debe ser mayor a 0 y hasta 100.");
        }

        if (await repository.HasActiveBonusAsync(request.DepartmentCode, request.EmployeeNumber, request.Period, cancellationToken))
            throw new BusinessException("BONUS_DUPLICATED", "El empleado ya tiene un bono activo en ese periodo.");

        var bonus = new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = request.Period,
            BonusTypeId = request.BonusTypeId,
            BonusValue = bonusValue,
            ReasonCode = request.ReasonCode,
            MonthlySalary = employee.MonthlySalary,
            TaxRate = employee.TaxRate,
            BonusStateCode = BonusStates.Registered,
            CreatedBy = userCode,
            CreatedAt = now
        };
        bonus.CalculateAmounts();

        repository.Add(bonus);
        await repository.SaveChangesAsync(cancellationToken);

        return bonus.BonusId;
    }

    public async Task CancelAsync(int bonusId, string userCode, CancellationToken cancellationToken)
    {
        var bonus = await GetRegisteredBonusAsync(bonusId, cancellationToken);
        bonus.BonusStateCode = BonusStates.Cancelled;
        bonus.ModifiedBy = userCode;
        bonus.ModifiedAt = DateTime.Now;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int bonusId, string userCode, CancellationToken cancellationToken)
    {
        var bonus = await GetRegisteredBonusAsync(bonusId, cancellationToken);
        bonus.DeletedBy = userCode;
        bonus.DeletedAt = DateTime.Now;
        await repository.SaveChangesAsync(cancellationToken);
    }

    // El reparto y el cambio a Pagado se guardan en un solo SaveChanges: EF Core lo ejecuta en una transacción.
    public async Task PayAsync(int bonusId, string userCode, CancellationToken cancellationToken)
    {
        var bonus = await GetRegisteredBonusAsync(bonusId, cancellationToken);
        var projects = await repository.GetProjectHoursAsync(bonus.DepartmentCode, bonus.EmployeeNumber, cancellationToken);
        bonus.Pay(projects, userCode, DateTime.Now);
        await repository.SaveChangesAsync(cancellationToken);
    }

    // Solo se aceptan empleados Activos (0101). También lo usa el registro del bono.
    private async Task<Employee> GetActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeAsync(departmentCode, employeeNumber, cancellationToken)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", "El empleado no existe.");

        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_TERMINATED", "El empleado no está activo.");

        return employee;
    }

    private async Task<Bonus> GetRegisteredBonusAsync(int bonusId, CancellationToken cancellationToken)
    {
        var bonus = await repository.GetByIdAsync(bonusId, cancellationToken);

        if (bonus is null || bonus.DeletedAt is not null)
            throw new BusinessException("BONUS_NOT_FOUND", "El bono no existe.");

        if (bonus.BonusStateCode != BonusStates.Registered)
            throw new BusinessException("BONUS_NOT_REGISTERED", "Solo se puede anular, eliminar o pagar un bono en estado Registrado.");

        return bonus;
    }
}
