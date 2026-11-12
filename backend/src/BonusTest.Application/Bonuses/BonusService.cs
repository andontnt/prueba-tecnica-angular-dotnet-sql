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
