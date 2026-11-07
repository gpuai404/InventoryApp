namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Base exception for failures caused by domain rules or invalid domain input.
/// Infrastructure exceptions must not derive from this type.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(
        string code,
        string message,
        string? propertyName = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("Exception code cannot be empty.", nameof(code))
            : code;
        PropertyName = propertyName;
    }

    public string Code { get; }

    public string? PropertyName { get; }
}

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(
        string code,
        string message,
        string? propertyName = null,
        Exception? innerException = null)
        : base(code, message, propertyName, innerException)
    {
    }
}

public sealed class DomainRuleViolationException : DomainException
{
    public DomainRuleViolationException(
        string code,
        string message,
        string? propertyName = null,
        Exception? innerException = null)
        : base(code, message, propertyName, innerException)
    {
    }
}
