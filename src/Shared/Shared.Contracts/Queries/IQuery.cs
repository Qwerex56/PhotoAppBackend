namespace Shared.Contracts.Queries;

/// <summary>
/// Base interface for queries processed via MediatR.
/// Queries represent intent to retrieve data without side effects.
/// </summary>
public interface IQuery<out TResponse>
{
    /// <summary>
    /// Unique correlation ID for tracking a query execution.
    /// </summary>
    Guid CorrelationId { get; }
}

/// <summary>
/// Base abstract class for queries.
/// </summary>
public abstract class Query<TResponse> : IQuery<TResponse>
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
