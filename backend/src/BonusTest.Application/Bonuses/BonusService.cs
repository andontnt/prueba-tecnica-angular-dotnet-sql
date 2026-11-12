using System.Globalization;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository)
{
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

        // Monto fijo: > 0 y <= sueldo. Porcentaje: > 0 y <= 100.
        if (request.BonusTypeId == BonusTypes.FixedAmount)
        {
            if (request.BonusValue <= 0 || request.BonusValue > employee.MonthlySalary)
                throw new BusinessException("AMOUNT_INVALID", "El monto debe ser mayor a 0 y no mayor al sueldo mensual.");
        }
        else if (request.BonusValue <= 0 || request.BonusValue > 100)
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
            BonusValue = request.BonusValue,
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

    // Solo se aceptan empleados Activos (0101). También lo usa el registro del bono.
    private async Task<Employee> GetActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeAsync(departmentCode, employeeNumber, cancellationToken)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", "El empleado no existe.");

        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_TERMINATED", "El empleado no está activo.");

        return employee;
    }
}
