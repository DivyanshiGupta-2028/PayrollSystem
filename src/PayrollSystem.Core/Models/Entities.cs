namespace PayrollSystem.Core.Models;

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Employee
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MonthlyAttendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public int TotalWorkingDays { get; set; }
    public decimal DaysPresent { get; set; }
    public string? Remarks { get; set; }
}

public class PayrollRun
{
    public int PayrollRunId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public DateTime RunDate { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalPFDeduction { get; set; }
    public decimal TotalProfessionalTax { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalDeductions => TotalPFDeduction + TotalProfessionalTax;
    public string Status { get; set; } = "Finalized";
    public string CreatedBy { get; set; } = "SYSTEM";
}

public class PayrollDetail
{
    public int PayrollDetailId { get; set; }
    public int PayrollRunId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
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
