using PayrollSystem.Core.DTOs;
using PayrollSystem.Core.Models;

namespace PayrollSystem.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int employeeId);
}

public interface IPayrollRepository
{
    Task<bool> PayrollRunExistsAsync(int month, int year);
    Task<int> RunPayrollAsync(int month, int year, string createdBy);
    Task<(PayrollRun? Run, IEnumerable<PayrollDetail> Details)> GetPayrollRunAsync(int month, int year);
    Task<PayslipDto?> GetEmployeePayslipAsync(int runId, int employeeId);
    Task<IEnumerable<PayrollRun>> GetAllPayrollRunsAsync();
}

public interface IPayrollService
{
    Task<PayrollRunSummaryDto> RunPayrollAsync(PayrollRunRequest request);
    Task<PayrollRunSummaryDto> GetPayrollRunAsync(int month, int year, int? page = null, int? pageSize = null);
    Task<PayslipDto> GetEmployeePayslipAsync(int runId, int employeeId);
    Task<IEnumerable<PayrollRun>> GetAllRunsAsync();
}

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
}

