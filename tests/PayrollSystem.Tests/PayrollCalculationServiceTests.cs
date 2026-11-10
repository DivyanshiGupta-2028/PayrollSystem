using PayrollSystem.Core.Services;
using Xunit;

namespace PayrollSystem.Tests;

public class PayrollCalculationServiceTests
{
    private readonly PayrollCalculationService _calculator = new();

    // Verification test for Ravi Sharma (EMP001): Basic 30,000, Working days 26, Days present 24 -> Net 23,892
    [Fact]
    public void Calculate_RaviSharmaExample_MatchesBriefSpecificationExactly()
    {
        // Arrange
        decimal basicSalary = 30000m;
        int workingDays = 26;
        decimal daysPresent = 24m;

        // Act
        var result = _calculator.Calculate(basicSalary, workingDays, daysPresent);

        // Assert
        Assert.Equal(30000m, result.BasicSalary);
        Assert.Equal(26, result.WorkingDays);
        Assert.Equal(24m, result.DaysPresent);
        Assert.Equal(27692m, result.GrossPay);
        Assert.Equal(3600m, result.PFDeduction);
        Assert.Equal(200m, result.ProfessionalTax);
        Assert.Equal(3800m, result.TotalDeductions);
        Assert.Equal(23892m, result.NetPay);
    }

    [Fact]
    public void Calculate_FullAttendance_GrossMatchesBasicSalary()
    {
        // Arrange
        decimal basicSalary = 45000m;
        int workingDays = 26;
        decimal daysPresent = 26m;

        // Act
        var result = _calculator.Calculate(basicSalary, workingDays, daysPresent);

        // Assert
        Assert.Equal(45000m, result.GrossPay);
        Assert.Equal(5400m, result.PFDeduction); // 12% of 45000
        Assert.Equal(200m, result.ProfessionalTax);
        Assert.Equal(39400m, result.NetPay); // 45000 - 5400 - 200
    }

    [Fact]
    public void Calculate_ZeroDaysPresent_ReturnsZeroPaySafely()
    {
        // Arrange: Absent whole month
        decimal basicSalary = 30000m;
        int workingDays = 26;
        decimal daysPresent = 0m;

        // Act
        var result = _calculator.Calculate(basicSalary, workingDays, daysPresent);

        // Assert
        Assert.Equal(0m, result.GrossPay);
        Assert.Equal(0m, result.PFDeduction);
        Assert.Equal(0m, result.ProfessionalTax);
        Assert.Equal(0m, result.NetPay);
    }

    [Fact]
    public void Calculate_ZeroWorkingDays_HandlesDivisionByZeroGracefully()
    {
        // Arrange
        decimal basicSalary = 30000m;
        int workingDays = 0;
        decimal daysPresent = 0m;

        // Act
        var result = _calculator.Calculate(basicSalary, workingDays, daysPresent);

        // Assert
        Assert.Equal(0m, result.GrossPay);
        Assert.Equal(0m, result.NetPay);
    }

    [Fact]
    public void Calculate_HalfDayAttendance_CalculatesAccurately()
    {
        // Arrange: 24.5 days present
        decimal basicSalary = 30000m;
        int workingDays = 26;
        decimal daysPresent = 24.5m;

        // Act
        var result = _calculator.Calculate(basicSalary, workingDays, daysPresent);

        // (30000 / 26) * 24.5 = 28269.2307... -> 28269
        Assert.Equal(28269m, result.GrossPay);
        Assert.Equal(3600m, result.PFDeduction);
        Assert.Equal(200m, result.ProfessionalTax);
        Assert.Equal(24469m, result.NetPay); // 28269 - 3600 - 200
    }

    [Fact]
    public void Calculate_NegativeBasicSalary_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(-5000m, 26, 20m));
    }

    [Fact]
    public void Calculate_NegativeWorkingDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(30000m, -10, 5m));
    }

    [Fact]
    public void Calculate_NegativeDaysPresent_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(30000m, 26, -2m));
    }

    [Fact]
    public void Calculate_DaysPresentExceedsWorkingDays_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _calculator.Calculate(30000m, 26, 28m));
    }

    [Theory]
    [InlineData(23892, "Twenty Three Thousand Eight Hundred Ninety Two Rupees Only")]
    [InlineData(30000, "Thirty Thousand Rupees Only")]
    [InlineData(105000, "One Lakh Five Thousand Rupees Only")]
    [InlineData(0, "Zero Rupees Only")]
    public void ConvertAmountToWords_ConvertsCorrectly(decimal amount, string expectedWords)
    {
        string actual = _calculator.ConvertAmountToWords(amount);
        Assert.Equal(expectedWords, actual);
    }
}

