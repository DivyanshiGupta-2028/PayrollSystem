namespace PayrollSystem.Core.Exceptions;

public class PayrollConflictException : Exception
{
    public int Month { get; }
    public int Year { get; }

    public PayrollConflictException(string message) : base(message) { }

    public PayrollConflictException(int month, int year)
        : base($"A finalized payroll run already exists for {month:D2}/{year}. Finalized payroll runs cannot be modified or re-run.")
    {
        Month = month;
        Year = year;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

