namespace PayrollSystem.Core.Interfaces;

public interface IPayrollCalculationService
{
    PayrollCalculationResult Calculate(decimal basicSalary, int totalWorkingDays, decimal daysPresent);
    string ConvertAmountToWords(decimal amount);
}

public class PayrollCalculationResult
{
    public decimal BasicSalary { get; set; }
    public int WorkingDays { get; set; }
    public decimal DaysPresent { get; set; }
    public decimal GrossPay { get; set; }
    public decimal PFDeduction { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TotalDeductions => PFDeduction + ProfessionalTax;
    public decimal NetPay { get; set; }
}

