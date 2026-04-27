namespace Ds3fsFileUploader.Models;

public class AppSettings
{
    public string BaseUrlApi        { get; set; } = "http://192.168.1.217:7076/api/v1";
    public string BucketName        { get; set; } = "test-ver-bkt";
    public string BaseUrlKeycloak   { get; set; } = "http://192.168.1.217:8087";
    public string Realm             { get; set; } = "Djems";
    public string UserName          { get; set; } = "minio1";
    public string Password          { get; set; } = "123";
    public string ClientId          { get; set; } = "minio-client";
    public string GrantType         { get; set; } = "password";
    public string ClientSecret      { get; set; } = "H4hJJygQoUxTIlRjn9pPNpijw4ho90CM";
    public string DestinationFolder { get; set; } = "";
}