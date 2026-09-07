namespace TNT.IdentityService.Api.Exceptions;

/// <summary>Thrown when a resource already exists (e.g. duplicate email).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Thrown when authentication or authorization fails.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>Thrown when a requested resource is not found.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
