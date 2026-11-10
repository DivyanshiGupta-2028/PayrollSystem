namespace PayrollSystem.Core.DTOs;

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public bool IsActive { get; set; }
}

public class PayrollRunRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string? CreatedBy { get; set; } = "HR Admin";
}

public class PayrollItemDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public int WorkingDays { get; set; }
    public decimal DaysPresent { get; set; }
    public decimal GrossPay { get; set; }
    public decimal PFDeduction { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal NetPay { get; set; }
}

public class PayrollRunSummaryDto
{
    public int PayrollRunId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime RunDate { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalPFDeduction { get; set; }
    public decimal TotalProfessionalTax { get; set; }
    public decimal TotalDeductions => TotalPFDeduction + TotalProfessionalTax;
    public decimal TotalNetPay { get; set; }
    public string Status { get; set; } = "Finalized";
    public string CreatedBy { get; set; } = string.Empty;
    public List<PayrollItemDto> Items { get; set; } = new();
    
    // Pagination metadata when paginated
    public PaginationMetadata? Pagination { get; set; }
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}

public class PayslipDto
{
    public int PayrollRunId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime RunDate { get; set; }
    public string Status { get; set; } = "Finalized";

    // Employee Info
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }

    // Attendance
    public int WorkingDays { get; set; }
    public decimal DaysPresent { get; set; }
    public decimal DaysAbsent { get; set; }

    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal GrossPay { get; set; }

    // Deductions
    public decimal PFDeduction { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TotalDeductions { get; set; }

    // Net Pay
    public decimal NetPay { get; set; }
    public string NetPayInWords { get; set; } = string.Empty;
}

