namespace Ds3fsFileUploader.Models
{
    public class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_expires_in")]
        public int RefreshExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("not-before-policy")]
        public int NotBeforePolicy { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("session_state")]
        public string SessionState { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string Scope { get; set; }
    }
}
