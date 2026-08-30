namespace TNT.IdentityService.Api.DTOs;

/// <summary>Standard error response returned for all API errors.</summary>
public class ErrorResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public List<string>? Errors { get; set; }
    public string? TraceId { get; set; }
}
