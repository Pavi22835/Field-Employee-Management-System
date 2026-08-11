namespace FEMS.Application.Common.Models;

/// <summary>Base type for predictable, client-facing application errors.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string>? Errors { get; }

    public AppException(string message, int statusCode = 400, IReadOnlyList<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.", 404) { }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Unauthorized.") : base(message, 401) { }
}

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "Forbidden.") : base(message, 403) { }
}
