using PayrollSystem.Core.DTOs;
using PayrollSystem.Core.Exceptions;
using PayrollSystem.Core.Interfaces;
using PayrollSystem.Core.Models;

namespace PayrollSystem.Core.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IPayrollCalculationService _calculationService;

    public PayrollService(IPayrollRepository payrollRepository, IPayrollCalculationService calculationService)
    {
        _payrollRepository = payrollRepository ?? throw new ArgumentNullException(nameof(payrollRepository));
        _calculationService = calculationService ?? throw new ArgumentNullException(nameof(calculationService));
    }

    public async Task<PayrollRunSummaryDto> RunPayrollAsync(PayrollRunRequest request)
    {
        // 1. Input Validation
        if (request == null)
            throw new ValidationException("Request body cannot be null.");

        if (request.Month < 1 || request.Month > 12)
            throw new ValidationException($"Invalid month '{request.Month}'. Month must be between 1 and 12.");

        if (request.Year < 2000 || request.Year > 2100)
            throw new ValidationException($"Invalid year '{request.Year}'. Year must be between 2000 and 2100.");

        // 2. Immutability & Conflict Check: Return HTTP 409 Conflict if already exists
        bool alreadyExists = await _payrollRepository.PayrollRunExistsAsync(request.Month, request.Year);
        if (alreadyExists)
        {
            throw new PayrollConflictException(request.Month, request.Year);
        }

        // 3. Trigger stored procedure usp_RunPayroll via Repository
        await _payrollRepository.RunPayrollAsync(request.Month, request.Year, request.CreatedBy ?? "HR Admin");

        // 4. Retrieve and return the created run
        return await GetPayrollRunAsync(request.Month, request.Year);
    }

    public async Task<PayrollRunSummaryDto> GetPayrollRunAsync(int month, int year, int? page = null, int? pageSize = null)
    {
        if (month < 1 || month > 12)
            throw new ValidationException($"Invalid month '{month}'. Month must be between 1 and 12.");

        if (year < 2000 || year > 2100)
            throw new ValidationException($"Invalid year '{year}'. Year must be between 2000 and 2100.");

        var (run, details) = await _payrollRepository.GetPayrollRunAsync(month, year);
        if (run == null)
        {
            throw new NotFoundException($"No finalized payroll run found for {month:D2}/{year}.");
        }

        var allItems = details.Select(d => new PayrollItemDto
        {
            EmployeeId = d.EmployeeId,
            EmployeeCode = d.EmployeeCode,
            Name = string.IsNullOrWhiteSpace(d.Name) ? $"{d.FirstName} {d.LastName}".Trim() : d.Name,
            DepartmentName = d.DepartmentName,
            Designation = d.Designation,
            BasicSalary = d.BasicSalary,
            WorkingDays = d.WorkingDays,
            DaysPresent = d.DaysPresent,
            GrossPay = d.GrossPay,
            PFDeduction = d.PFDeduction,
            ProfessionalTax = d.ProfessionalTax,
            NetPay = d.NetPay
        }).ToList();

        List<PayrollItemDto> itemsToReturn = allItems;
        PaginationMetadata? pagination = null;

        // Bonus Deliverable: Pagination support
        if (page.HasValue && pageSize.HasValue && pageSize.Value > 0)
        {
            int currentPage = Math.Max(1, page.Value);
            int size = pageSize.Value;
            int total = allItems.Count;

            itemsToReturn = allItems
                .Skip((currentPage - 1) * size)
                .Take(size)
                .ToList();

            pagination = new PaginationMetadata
            {
                CurrentPage = currentPage,
                PageSize = size,
                TotalItems = total
            };
        }

        return new PayrollRunSummaryDto
        {
            PayrollRunId = run.PayrollRunId,
            Month = run.PayrollMonth,
            Year = run.PayrollYear,
            RunDate = run.RunDate,
            TotalEmployees = run.TotalEmployees,
            TotalGrossPay = run.TotalGrossPay,
            TotalPFDeduction = run.TotalPFDeduction,
            TotalProfessionalTax = run.TotalProfessionalTax,
            TotalNetPay = run.TotalNetPay,
            Status = run.Status,
            CreatedBy = run.CreatedBy,
            Items = itemsToReturn,
            Pagination = pagination
        };
    }

    public async Task<PayslipDto> GetEmployeePayslipAsync(int runId, int employeeId)
    {
        if (runId <= 0)
            throw new ValidationException("Invalid runId. Must be greater than 0.");

        if (employeeId <= 0)
            throw new ValidationException("Invalid employeeId. Must be greater than 0.");

        var payslip = await _payrollRepository.GetEmployeePayslipAsync(runId, employeeId);
        if (payslip == null)
        {
            throw new NotFoundException($"Payslip not found for Employee ID {employeeId} in Payroll Run ID {runId}.");
        }

        // Add amount in words for official printable payslip
        payslip.NetPayInWords = _calculationService.ConvertAmountToWords(payslip.NetPay);

        return payslip;
    }

    public async Task<IEnumerable<PayrollRun>> GetAllRunsAsync()
    {
        return await _payrollRepository.GetAllPayrollRunsAsync();
    }
}

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(e => new EmployeeDto
        {
            EmployeeId = e.EmployeeId,
            EmployeeCode = e.EmployeeCode,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Name = e.Name,
            Email = e.Email,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.DepartmentName,
            Designation = e.Designation,
            BasicSalary = e.BasicSalary,
            DateOfJoining = e.DateOfJoining,
            IsActive = e.IsActive
        });
    }
}
