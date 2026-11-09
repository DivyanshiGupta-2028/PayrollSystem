using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using PayrollSystem.Core.DTOs;
using PayrollSystem.Core.Exceptions;
using PayrollSystem.Core.Interfaces;
using PayrollSystem.Core.Models;
using PayrollSystem.Infrastructure.Data;

namespace PayrollSystem.Infrastructure.Repositories;

public class PayrollRepository : IPayrollRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PayrollRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<bool> PayrollRunExistsAsync(int month, int year)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM dbo.PayrollRuns WHERE PayrollMonth = @Month AND PayrollYear = @Year;";
        int count = await connection.ExecuteScalarAsync<int>(sql, new { Month = month, Year = year });
        return count > 0;
    }

    public async Task<int> RunPayrollAsync(int month, int year, string createdBy)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Month", month, DbType.Int32);
            parameters.Add("@Year", year, DbType.Int32);
            parameters.Add("@CreatedBy", createdBy, DbType.String);

            return await connection.ExecuteScalarAsync<int>(
                "dbo.usp_RunPayroll",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        catch (SqlException ex) when (ex.Number == 51000 || ex.Message.Contains("PAYROLL_ALREADY_EXISTS"))
        {
            throw new PayrollConflictException(month, year);
        }
    }

    public async Task<(PayrollRun? Run, IEnumerable<PayrollDetail> Details)> GetPayrollRunAsync(int month, int year)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Month", month, DbType.Int32);
        parameters.Add("@Year", year, DbType.Int32);

        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_GetPayrollRun",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var run = await multi.ReadFirstOrDefaultAsync<PayrollRun>();
        var details = (await multi.ReadAsync<PayrollDetail>()).ToList();

        return (run, details);
    }

    public async Task<PayslipDto?> GetEmployeePayslipAsync(int runId, int employeeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@RunId", runId, DbType.Int32);
        parameters.Add("@EmployeeId", employeeId, DbType.Int32);

        return await connection.QueryFirstOrDefaultAsync<PayslipDto>(
            "dbo.usp_GetEmployeePayslip",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<PayrollRun>> GetAllPayrollRunsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PayrollRun>(
            "dbo.usp_GetAllPayrollRuns",
            commandType: CommandType.StoredProcedure
        );
    }
}
