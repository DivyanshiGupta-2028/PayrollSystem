using PayrollSystem.Core.DTOs;
using PayrollSystem.Core.Exceptions;
using PayrollSystem.Core.Interfaces;
using PayrollSystem.Core.Models;
using PayrollSystem.Core.Services;
using Xunit;

namespace PayrollSystem.Tests;

public class PayrollServiceTests
{
    private readonly FakePayrollRepository _fakeRepository = new();
    private readonly PayrollCalculationService _calculator = new();
    private readonly PayrollService _service;

    public PayrollServiceTests()
    {
        _service = new PayrollService(_fakeRepository, _calculator);
    }

    [Fact]
    public async Task RunPayrollAsync_DuplicateRun_ThrowsPayrollConflictException()
    {
        // Arrange
        _fakeRepository.ExistingRuns.Add((11, 2026));

        var request = new PayrollRunRequest { Month = 11, Year = 2026 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PayrollConflictException>(() =>
            _service.RunPayrollAsync(request));

        Assert.Equal(11, ex.Month);
        Assert.Equal(2026, ex.Year);
        Assert.Contains("already exists", ex.Message);
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(5, 1999)]
    [InlineData(5, 2200)]
    public async Task RunPayrollAsync_InvalidMonthOrYear_ThrowsValidationException(int month, int year)
    {
        var request = new PayrollRunRequest { Month = month, Year = year };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RunPayrollAsync(request));
    }

    [Fact]
    public async Task GetPayrollRunAsync_RunDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetPayrollRunAsync(5, 2025));
    }

    [Fact]
    public async Task GetPayrollRunAsync_WithPagination_ReturnsPaginatedItemsAndMetadata()
    {
        // Arrange: seed a run with 6 items
        _fakeRepository.SeedPayrollRun(11, 2026, 6);

        // Act: request page 1 with pageSize 2
        var result = await _service.GetPayrollRunAsync(11, 2026, page: 1, pageSize: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination.CurrentPage);
        Assert.Equal(2, result.Pagination.PageSize);
        Assert.Equal(6, result.Pagination.TotalItems);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasNext);
        Assert.False(result.Pagination.HasPrevious);
    }

    [Fact]
    public async Task GetEmployeePayslipAsync_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetEmployeePayslipAsync(runId: 999, employeeId: 999));
    }

    [Fact]
    public async Task GetEmployeePayslipAsync_Found_PopulatesNetPayInWords()
    {
        // Arrange
        _fakeRepository.SeedPayslip(1, 1, new PayslipDto
        {
            PayrollRunId = 1,
            EmployeeId = 1,
            Name = "Ravi Sharma",
            GrossPay = 27692m,
            PFDeduction = 3600m,
            ProfessionalTax = 200m,
            NetPay = 23892m
        });

        // Act
        var result = await _service.GetEmployeePayslipAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ravi Sharma", result.Name);
        Assert.Equal(23892m, result.NetPay);
        Assert.Equal("Twenty Three Thousand Eight Hundred Ninety Two Rupees Only", result.NetPayInWords);
    }
}

/// <summary>
/// Test Double for IPayrollRepository
/// </summary>
internal class FakePayrollRepository : IPayrollRepository
{
    public HashSet<(int Month, int Year)> ExistingRuns { get; } = new();
    private readonly Dictionary<(int Month, int Year), (PayrollRun Run, List<PayrollDetail> Details)> _runs = new();
    private readonly Dictionary<(int RunId, int EmployeeId), PayslipDto> _payslips = new();

    public void SeedPayrollRun(int month, int year, int itemCount)
    {
        ExistingRuns.Add((month, year));
        var run = new PayrollRun
        {
            PayrollRunId = 1,
            PayrollMonth = month,
            PayrollYear = year,
            RunDate = DateTime.UtcNow,
            TotalEmployees = itemCount,
            TotalNetPay = 100000m
        };

        var details = new List<PayrollDetail>();
        for (int i = 1; i <= itemCount; i++)
        {
            details.Add(new PayrollDetail
            {
                EmployeeId = i,
                EmployeeCode = $"EMP00{i}",
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Name = $"First{i} Last{i}",
                BasicSalary = 30000m,
                WorkingDays = 26,
                DaysPresent = 24m,
                GrossPay = 27692m,
                PFDeduction = 3600m,
                ProfessionalTax = 200m,
                NetPay = 23892m
            });
        }

        _runs[(month, year)] = (run, details);
    }

    public void SeedPayslip(int runId, int employeeId, PayslipDto dto)
    {
        _payslips[(runId, employeeId)] = dto;
    }

    public Task<bool> PayrollRunExistsAsync(int month, int year)
    {
        return Task.FromResult(ExistingRuns.Contains((month, year)));
    }

    public Task<int> RunPayrollAsync(int month, int year, string createdBy)
    {
        ExistingRuns.Add((month, year));
        SeedPayrollRun(month, year, 1);
        return Task.FromResult(1);
    }

    public Task<(PayrollRun? Run, IEnumerable<PayrollDetail> Details)> GetPayrollRunAsync(int month, int year)
    {
        if (_runs.TryGetValue((month, year), out var tuple))
        {
            return Task.FromResult<(PayrollRun?, IEnumerable<PayrollDetail>)>((tuple.Run, tuple.Details));
        }

        return Task.FromResult<(PayrollRun?, IEnumerable<PayrollDetail>)>((null, Enumerable.Empty<PayrollDetail>()));
    }

    public Task<PayslipDto?> GetEmployeePayslipAsync(int runId, int employeeId)
    {
        _payslips.TryGetValue((runId, employeeId), out var slip);
        return Task.FromResult(slip);
    }

    public Task<IEnumerable<PayrollRun>> GetAllPayrollRunsAsync()
    {
        return Task.FromResult<IEnumerable<PayrollRun>>(_runs.Values.Select(v => v.Run));
    }
}

