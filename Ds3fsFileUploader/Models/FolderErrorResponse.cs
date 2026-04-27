namespace Ds3fsFileUploader.Models
{
    public class FolderErrorResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public object Data { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public string Error { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
    }
}
