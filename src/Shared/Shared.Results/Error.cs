namespace Shared.Results;

/// <summary>
/// Represents an error that occurred during an operation.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Description { get; }

    private Error(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Creates a new Error with the specified code and description.
    /// </summary>
    public static Error Create(string code, string description) 
        => new(code, description);

    /// <summary>
    /// Common validation error.
    /// </summary>
    public static Error ValidationError(string description = "Validation failed") 
        => Create("VALIDATION_ERROR", description);

    /// <summary>
    /// Resource not found error.
    /// </summary>
    public static Error NotFound(string resource) 
        => Create("NOT_FOUND", $"{resource} not found");

    /// <summary>
    /// Unauthorized access error.
    /// </summary>
    public static Error Unauthorized(string description = "Unauthorized") 
        => Create("UNAUTHORIZED", description);

    /// <summary>
    /// Forbidden access error.
    /// </summary>
    public static Error Forbidden(string description = "Access denied") 
        => Create("FORBIDDEN", description);

    /// <summary>
    /// Conflict error (e.g., duplicate resource).
    /// </summary>
    public static Error Conflict(string description) 
        => Create("CONFLICT", description);

    /// <summary>
    /// Internal server error.
    /// </summary>
    public static Error InternalError(string description = "An internal error occurred") 
        => Create("INTERNAL_ERROR", description);

    /// <summary>
    /// Bad request error.
    /// </summary>
    public static Error BadRequest(string description) 
        => Create("BAD_REQUEST", description);
}
