namespace Shared.Contracts.Events;

/// <summary>
/// Base interface for domain events published via MassTransit.
/// All events must implement this interface for consistency.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique correlation ID for tracking related events across services.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// The service/aggregate that emitted this event.
    /// </summary>
    string Source { get; }
}

/// <summary>
/// Base abstract class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
}
