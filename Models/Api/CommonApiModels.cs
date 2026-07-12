namespace SipCoreMobile.Models.Api;

public class ApiSuccessResponse
{
    public bool Success { get; set; }
}

public class SimpleSuccessResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ChangePasswordRequest
{
    public required string Extension { get; set; }
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
}

public class PresenceRequest
{
    public required string Extension { get; set; }
}

public class FirebaseTokenRequest
{
    public required string Extension { get; set; }
    public required string Token { get; set; }
    public required string TokenType { get; set; }
    public required string Platform { get; set; }
}

public class MediaUploadResponse
{
    public bool Success { get; set; }
    public required string Url { get; set; }
    public required string FileName { get; set; }
    public string? MimeType { get; set; }
    public int? Duration { get; set; }
}
