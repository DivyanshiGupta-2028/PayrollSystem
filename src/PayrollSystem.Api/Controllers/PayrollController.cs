using Microsoft.AspNetCore.Mvc;
using PayrollSystem.Core.DTOs;
using PayrollSystem.Core.Interfaces;

namespace PayrollSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
    }

    /// <summary>
    /// POST /api/payroll/run
    /// Trigger the payroll run for a specified month and year.
    /// Body: { month, year }
    /// Expected response: 201 Created — run summary (or 409 Conflict if already exists)
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(PayrollRunSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RunPayroll([FromBody] PayrollRunRequest request)
    {
        var summary = await _payrollService.RunPayrollAsync(request);
        return StatusCode(StatusCodes.Status201Created, summary);
    }

    /// <summary>
    /// GET /api/payroll/{month}/{year}
    /// Return the saved payroll run for a given month and year.
    /// Expected response: 200 OK or 404 Not Found
    /// Supports optional query parameters ?page=1&pageSize=10 for pagination.
    /// </summary>
    [HttpGet("{month:int}/{year:int}")]
    [ProducesResponseType(typeof(PayrollRunSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayrollRun(
        [FromRoute] int month,
        [FromRoute] int year,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _payrollService.GetPayrollRunAsync(month, year, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/payroll/{runId}/slip/{employeeId}
    /// Return the individual payslip for one employee.
    /// Expected response: 200 OK or 404 Not Found
    /// </summary>
    [HttpGet("{runId:int}/slip/{employeeId:int}")]
    [ProducesResponseType(typeof(PayslipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeePayslip([FromRoute] int runId, [FromRoute] int employeeId)
    {
        var payslip = await _payrollService.GetEmployeePayslipAsync(runId, employeeId);
        return Ok(payslip);
    }

    /// <summary>
    /// GET /api/payroll/history
    /// Returns historical payroll runs to easily inspect past periods.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayrollHistory()
    {
        var runs = await _payrollService.GetAllRunsAsync();
        return Ok(runs);
    }
}

