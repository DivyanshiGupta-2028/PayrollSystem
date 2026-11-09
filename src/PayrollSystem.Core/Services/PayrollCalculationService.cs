using PayrollSystem.Core.Interfaces;

namespace PayrollSystem.Core.Services;

public class PayrollCalculationService : IPayrollCalculationService
{
    private const decimal PfRate = 0.12m;
    private const decimal StandardProfessionalTax = 200.00m;

    /// <summary>
    /// Calculates payroll components for an employee:
    /// - Gross Pay = (Basic Salary / Total Working Days) * Days Present
    /// - PF Deduction = 12% of Basic Salary
    /// - Professional Tax = Flat ₹200 per month
    /// - Net Pay = Gross Pay - PF - Professional Tax
    /// </summary>
    public PayrollCalculationResult Calculate(decimal basicSalary, int totalWorkingDays, decimal daysPresent)
    {
        if (basicSalary < 0)
            throw new ArgumentOutOfRangeException(nameof(basicSalary), "Basic salary cannot be negative.");

        if (totalWorkingDays < 0)
            throw new ArgumentOutOfRangeException(nameof(totalWorkingDays), "Total working days cannot be negative.");

        if (daysPresent < 0)
            throw new ArgumentOutOfRangeException(nameof(daysPresent), "Days present cannot be negative.");

        if (daysPresent > totalWorkingDays && totalWorkingDays > 0)
            throw new ArgumentException("Days present cannot exceed total working days.", nameof(daysPresent));

        // Edge case: 0 working days or 0 days present
        if (totalWorkingDays == 0 || daysPresent == 0)
        {
            return new PayrollCalculationResult
            {
                BasicSalary = Math.Round(basicSalary, 0, MidpointRounding.AwayFromZero),
                WorkingDays = totalWorkingDays,
                DaysPresent = daysPresent,
                GrossPay = 0m,
                PFDeduction = 0m,
                ProfessionalTax = 0m,
                NetPay = 0m
            };
        }

        // Gross Pay: (Basic Salary / Total Working Days) * Days Present
        decimal perDayRate = basicSalary / totalWorkingDays;
        decimal grossPay = Math.Round(perDayRate * daysPresent, 0, MidpointRounding.AwayFromZero);

        // PF Deduction: 12% of Basic Salary
        decimal pfDeduction = Math.Round(basicSalary * PfRate, 0, MidpointRounding.AwayFromZero);

        // Professional Tax: Flat ₹200
        decimal professionalTax = StandardProfessionalTax;

        // Net Pay: Gross Pay - PF - Professional Tax
        decimal netPay = grossPay - pfDeduction - professionalTax;

        // Safety Guard: In case gross pay is insufficient to cover statutory deductions,
        // Net pay does not drop below zero in payroll disbursement
        if (netPay < 0m)
        {
            netPay = 0m;
        }

        return new PayrollCalculationResult
        {
            BasicSalary = Math.Round(basicSalary, 0, MidpointRounding.AwayFromZero),
            WorkingDays = totalWorkingDays,
            DaysPresent = daysPresent,
            GrossPay = grossPay,
            PFDeduction = pfDeduction,
            ProfessionalTax = professionalTax,
            NetPay = netPay
        };
    }

    /// <summary>
    /// Converts a currency amount to English words (Indian format: Lakhs, Crores).
    /// Used for official printable payslips.
    /// </summary>
    public string ConvertAmountToWords(decimal amount)
    {
        long wholeNumber = (long)Math.Floor(amount);
        if (wholeNumber == 0) return "Zero Rupees Only";

        string words = NumberToWords(wholeNumber);
        return $"{words} Rupees Only";
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

        string words = "";

        if (number / 10000000 > 0)
        {
            words += NumberToWords(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if (number / 100000 > 0)
        {
            words += NumberToWords(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if (number / 1000 > 0)
        {
            words += NumberToWords(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += NumberToWords(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.Trim();
    }
}
