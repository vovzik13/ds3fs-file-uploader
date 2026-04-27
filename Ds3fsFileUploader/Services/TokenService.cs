using System.Net;
using System.Net.Http.Headers;

namespace Ds3fsFileUploader.Services;

/// <summary>
/// Сервис для управления токенами доступа с автоматическим обновлением
/// </summary>
public class TokenService : IDisposable
{
    private readonly Func<Task<string>> _getTokenFunc;
    private          string?            _cachedToken;
    private          DateTime           _tokenExpirationTime;
    private readonly SemaphoreSlim      _semaphore = new(1, 1);
    private          bool               _disposed;

    // Буфер времени до истечения токена (в секундах) - обновляем токен заранее
    private const int TokenRefreshBufferSeconds = 30;

    public TokenService(Func<Task<string>> getTokenFunc)
    {
        _getTokenFunc        = getTokenFunc ?? throw new ArgumentNullException(nameof(getTokenFunc));
        _tokenExpirationTime = DateTime.MinValue;
    }

    /// <summary>
    /// Получить актуальный токен доступа
    /// Если токен истек или скоро истечет, будет получен новый
    /// </summary>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TokenService));

        // Проверяем, нужно ли обновить токен
        if (IsTokenExpiredOrSoonToExpire())
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Двойная проверка после получения блокировки (чтобы избежать гонки)
                if (IsTokenExpiredOrSoonToExpire())
                {
                    _cachedToken = await _getTokenFunc();
                    // Устанавливаем время истечения (берем из ответа API или используем дефолтное значение)
                    // По умолчанию считаем что токен живет 5 минут, если не указано иное
                    _tokenExpirationTime = DateTime.UtcNow.AddMinutes(5);

                    Console.WriteLine($"[TokenService] Токен обновлен. Истекает в {_tokenExpirationTime:HH:mm:ss}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _cachedToken ?? throw new InvalidOperationException("Токен не был получен");
    }

    /// <summary>
    /// Установить время истечения токена (вызывается после получения токена из API)
    /// </summary>
    public void SetTokenExpiration(int expiresInSeconds)
    {
        _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresInSeconds - TokenRefreshBufferSeconds);
        Console.WriteLine($"[TokenService] Время жизни токена установлено: {expiresInSeconds} сек. Обновление в {_tokenExpirationTime:HH:mm:ss}");
    }

    /// <summary>
    /// Принудительно инвалидировать текущий токен
    /// </summary>
    public void InvalidateToken()
    {
        _cachedToken         = null;
        _tokenExpirationTime = DateTime.MinValue;
        Console.WriteLine("[TokenService] Токен инвалидирован");
    }

    /// <summary>
    /// Проверить, истек ли токен или скоро истечет
    /// </summary>
    private bool IsTokenExpiredOrSoonToExpire()
    {
        return string.IsNullOrEmpty(_cachedToken) ||
               DateTime.UtcNow >= _tokenExpirationTime;
    }

    /// <summary>
    /// Обработчик для HttpClient, который автоматически обновляет токен при получении 401 ошибки
    /// </summary>
    public class RefreshTokenHandler : DelegatingHandler
    {
        private readonly TokenService _tokenService;
        private readonly Action<string> _logAction;

        public RefreshTokenHandler(TokenService tokenService, Action<string> logAction)
            : this(tokenService, new HttpClientHandler(), logAction)
        {
        }

        public RefreshTokenHandler(TokenService tokenService, HttpMessageHandler innerHandler, Action<string> logAction)
            : base(innerHandler)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logAction    = logAction ?? throw new ArgumentNullException(nameof(logAction));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Устанавливаем токен в запрос
            var token = await _tokenService.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Отправляем запрос
            var response = await base.SendAsync(request, cancellationToken);

            // Если получили 401 - пробуем обновить токен и повторить запрос
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logAction($"[TokenService] Получена ошибка 401. Попытка обновления токена...");
                Console.WriteLine("[TokenService] Получена ошибка 401. Обновляем токен...");

                // Инвалидируем текущий токен
                _tokenService.InvalidateToken();

                // Получаем новый токен
                var newToken = await _tokenService.GetTokenAsync(cancellationToken);

                // Повторяем запрос с новым токеном
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                // Создаем новый запрос для повторной отправки
                using var retryRequest = await CloneRequestAsync(request);
                response.Dispose();

                response = await base.SendAsync(retryRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logAction("[TokenService] Запрос успешно выполнен после обновления токена");
                    Console.WriteLine("[TokenService] Запрос успешно выполнен после обновления токена");
                }
                else
                {
                    // Логируем любую ошибку после повторной попытки (не только 401)
                    _logAction($"[TokenService] Повторная попыка с новым токеном вернула ошибку {(int)response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"[TokenService] Повторная попытка также вернула ошибку {(int)response.StatusCode}. Токен недействителен или другая ошибка.");
                }
            }

            return response;
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage originalRequest)
        {
            var clone = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);

            // Копируем заголовки
            foreach (var header in originalRequest.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Копируем содержимое если оно есть
            if (originalRequest.Content != null)
            {
                var memoryStream = new MemoryStream();
                await originalRequest.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                clone.Content = new StreamContent(memoryStream);

                // Копируем заголовки контента
                foreach (var header in originalRequest.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}