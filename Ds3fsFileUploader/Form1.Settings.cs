using Ds3fsFileUploader.Models;

namespace Ds3fsFileUploader;

public partial class FrmMain
{
    private const string      SettingsFile = "appsettings.json";
    private       AppSettings _settings    = new();

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                _settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }

            ApplySettingsToUi();
        }
        catch (Exception ex)
        {
            LogMessage($"Ошибка загрузки настроек: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        try
        {
            GetSettingsFromUi();
            var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
            MessageBox.Show(@"Настройки сохранены!", @"Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogMessage($@"Ошибка сохранения настроек: {ex.Message}");
            MessageBox.Show($@"Ошибка сохранения настроек: {ex.Message}", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplySettingsToUi()
    {
        tb_BaseUrlApi.Text      = _settings.BaseUrlApi;
        tb_BucketName.Text      = _settings.BucketName;
        tb_BaseUrlKeycloak.Text = _settings.BaseUrlKeycloak;
        tb_Realm.Text           = _settings.Realm;
        tb_UserName.Text        = _settings.UserName;
        tb_Password.Text        = _settings.Password;
        tb_ClientId.Text        = _settings.ClientId;
        tb_GrantType.Text       = _settings.GrantType;
        tb_ClientSecret.Text    = _settings.ClientSecret;
        tb_Destination.Text     = _settings.DestinationFolder;

        UpdateUrls(); // Обновляем URL после применения настроек
    }

    private void GetSettingsFromUi()
    {
        _settings.BaseUrlApi        = tb_BaseUrlApi.Text;
        _settings.BucketName        = tb_BucketName.Text;
        _settings.BaseUrlKeycloak   = tb_BaseUrlKeycloak.Text;
        _settings.Realm             = tb_Realm.Text;
        _settings.UserName          = tb_UserName.Text;
        _settings.Password          = tb_Password.Text;
        _settings.ClientId          = tb_ClientId.Text;
        _settings.GrantType         = tb_GrantType.Text;
        _settings.ClientSecret      = tb_ClientSecret.Text;
        _settings.DestinationFolder = tb_Destination.Text;
    }

    /// <summary>
    /// Доступ к настройкам из основного класса
    /// </summary>
    private AppSettings Settings => _settings;

    private void btSaveSettings_Click(object sender, EventArgs e)
    {
        SaveSettings();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Авто сохранение при закрытии
        SaveSettings();
        base.OnFormClosing(e);
    }

    private bool ValidateRequiredFields()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(tb_BaseUrlApi.Text))
            errors.Add("URL API ФХ не заполнен");

        if (string.IsNullOrWhiteSpace(tb_BucketName.Text))
            errors.Add("Название бакета не заполнено");

        if (string.IsNullOrWhiteSpace(tb_BaseUrlKeycloak.Text))
            errors.Add("URL Keycloak не заполнен");

        if (string.IsNullOrWhiteSpace(tb_Realm.Text))
            errors.Add("Realm не заполнен");

        if (string.IsNullOrWhiteSpace(tb_UserName.Text))
            errors.Add("UserName не заполнен");

        if (string.IsNullOrWhiteSpace(tb_ClientId.Text))
            errors.Add("Client Id не заполнен");

        if (string.IsNullOrWhiteSpace(tb_ClientSecret.Text))
            errors.Add("Client Secret не заполнен");

        if (string.IsNullOrWhiteSpace(tb_SourceFolder.Text))
            errors.Add("Не выбрана папка для копирования");

        if (string.IsNullOrWhiteSpace(tb_Destination.Text))
            errors.Add("Не указан путь в бакете (например: folder1/folder2/)");

        // Проверка валидности URL
        if (!Uri.TryCreate(tb_BaseUrlApi.Text, UriKind.Absolute, out _))
            errors.Add("URL API ФХ имеет неверный формат");

        if (!Uri.TryCreate(tb_BaseUrlKeycloak.Text, UriKind.Absolute, out _))
            errors.Add("URL Keycloak имеет неверный формат");

        if (errors.Count <= 0)
            return true;

        MessageBox.Show(@"Обнаружены ошибки:\n- " + string.Join("\n- ", errors),
            @"Проверка полей", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        return false;
    }

    private void UpdateUrls()
    {
        _putObjectUrl       = $"{tb_BaseUrlApi.Text}/Objects/PutObject/{tb_BucketName.Text}";
        _createFolderUrl    = $"{tb_BaseUrlApi.Text}/Objects/CreateFolder/{tb_BucketName.Text}";
        _checkFileExistsUrl = $"{tb_BaseUrlApi.Text}/Objects/CheckFileExists/{tb_BucketName.Text}";
        _tokenUrl           = $"{tb_BaseUrlKeycloak.Text}/realms/{tb_Realm.Text}/protocol/openid-connect/token";
    }
}