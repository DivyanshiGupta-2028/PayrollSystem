using System.Data;
using Dapper;
using PayrollSystem.Core.Interfaces;
using PayrollSystem.Core.Models;
using PayrollSystem.Infrastructure.Data;

namespace PayrollSystem.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmployeeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Employee>(
            "dbo.usp_GetEmployees",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Employee?> GetByIdAsync(int employeeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                e.EmployeeId,
                e.EmployeeCode,
                e.FirstName,
                e.LastName,
                (e.FirstName + ' ' + e.LastName) AS Name,
                e.Email,
                e.DepartmentId,
                d.DepartmentName,
                e.Designation,
                e.BasicSalary,
                e.DateOfJoining,
                e.IsActive,
                e.CreatedAt
            FROM dbo.Employees e
            INNER JOIN dbo.Departments d ON e.DepartmentId = d.DepartmentId
            WHERE e.EmployeeId = @EmployeeId;";

        return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeId = employeeId });
    }
}

