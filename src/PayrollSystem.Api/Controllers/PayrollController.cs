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

    // POST /api/payroll/run
    [HttpPost("run")]
    [ProducesResponseType(typeof(PayrollRunSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RunPayroll([FromBody] PayrollRunRequest request)
    {
        var summary = await _payrollService.RunPayrollAsync(request);
        return StatusCode(StatusCodes.Status201Created, summary);
    }

    // GET /api/payroll/{month}/{year}
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

    // GET /api/payroll/{runId}/payslip/{employeeId}
    [HttpGet("{runId:int}/payslip/{employeeId:int}")]
    [ProducesResponseType(typeof(PayslipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeePayslip([FromRoute] int runId, [FromRoute] int employeeId)
    {
        var payslip = await _payrollService.GetEmployeePayslipAsync(runId, employeeId);
        return Ok(payslip);
    }

    // GET /api/payroll/history
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayrollHistory()
    {
        var runs = await _payrollService.GetAllRunsAsync();
        return Ok(runs);
    }
}

