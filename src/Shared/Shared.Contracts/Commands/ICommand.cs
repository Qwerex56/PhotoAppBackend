namespace Shared.Contracts.Commands;

/// <summary>
/// Base interface for commands processed via MediatR.
/// Commands represent intent to change state.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique correlation ID for tracking a command execution across services.
    /// </summary>
    Guid CorrelationId { get; }
}

/// <summary>
/// Base interface for commands that return a result.
/// </summary>
public interface ICommand<out TResponse> : ICommand
{
}

/// <summary>
/// Base abstract class for commands.
/// </summary>
public abstract class Command : ICommand
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Base abstract class for commands with response.
/// </summary>
public abstract class Command<TResponse> : ICommand<TResponse>
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
