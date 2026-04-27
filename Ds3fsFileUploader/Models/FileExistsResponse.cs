namespace Ds3fsFileUploader.Models;

public class FileExistsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("exists")]
    public bool Exists { get; set; }
        
    [System.Text.Json.Serialization.JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
        
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
        
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
        
    [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }
}