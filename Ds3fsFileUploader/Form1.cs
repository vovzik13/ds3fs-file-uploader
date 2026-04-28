using System.Net.Http.Headers;
using Ds3fsFileUploader.Models;
using Ds3fsFileUploader.Services;
using Microsoft.Win32;
using static System.Text.Json.JsonSerializer;
using FormsApplication = System.Windows.Forms.Application;

namespace Ds3fsFileUploader
{
    public partial class FrmMain : Form
    {
        private CancellationTokenSource _cancellationTokenSource = null!;
        private bool                    _isRunning;
        private TokenService?           _tokenService;
        private DateTime                _operationStartTime;
        private long                    _totalBytesUploaded;
        private long                    _totalSourceFolderSize;

        // Переменные для параллельной загрузки
        private SemaphoreSlim _uploadSemaphore = null!;
        private List<FileUploadSlot> _uploadSlots = null!;
        private object _slotsLock = new object();

        // Куда копировать
        private string _destinationUrl = null!;

        // Константы ФХ
        private static string _putObjectUrl       = null!;
        private static string _createFolderUrl    = null!;
        private static string _checkFileExistsUrl = null!;

        // URL для получения токена
        private static string _tokenUrl = null!;

        // Шаблоны для исключения файлов
        private static readonly string[] ExcludePatterns = []; //["*.txt", "*desktop*"]

        // Список пустых папок
        private static readonly List<string> EmptyFolders = [];

        // Логгер
        private static StreamWriter _logWriter = null!;

        public FrmMain()
        {
            // Инициализация лог-файла
            var logFileName = $"upload_{DateTime.Now:yyyy-MM-dd}.log";
            _logWriter = new StreamWriter(logFileName, append: true);

            InitializeComponent();

            // Инициализируем обработчики событий
            InitializeEventHandlers();

            // Загружаем настройки
            LoadSettings();
            
            // Инициализируем слоты для загрузки
            InitializeUploadSlots();
        }

        private void btChoiceFolder_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();

            // выбрал ли пользователь папку
            if (result != DialogResult.OK)
                return;

            // Получите путь к выбранной папке
            tb_SourceFolder.Text  = folderBrowserDialog1.SelectedPath;
            btGetFileList.Enabled = true;
            
            // Вычисляем общий размер файлов в папке
            _totalSourceFolderSize = CalculateTotalFolderSize(tb_SourceFolder.Text);
            label1.Text = $"Источник: {FormatFileSize(_totalSourceFolderSize)}";
        }

        private async void btGetFileList_Click(object sender, EventArgs e)
        {
            // Если процесс уже запущен - останавливаем
            if (_isRunning)
            {
                StopProcess();
                return;
            }

            // Проверка обязательных полей
            if (!ValidateRequiredFields())
                return;

            // Обновляем URL перед началом процесса
            UpdateUrls();

            _destinationUrl = tb_Destination.Text.EndsWith('/') ? tb_Destination.Text : $"{tb_Destination.Text}/";

            // Запускаем процесс
            await StartProcess();
        }

        private async Task StartProcess()
        {
            _isRunning               = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Инициализируем переменные для отслеживания
            _operationStartTime = DateTime.Now;
            _totalBytesUploaded = 0;

            // Запускаем таймер
            timerElapsed.Start();

            // Обновляем кнопку
            UpdateStartStopButton(true);

            // Блокируем форму
            SetFormEnabled(false);

            // Разблокируем кнопку остановки
            btGetFileList.Enabled = true;

            try
            {
                await Task.Run(() => RunUploadProcess(_cancellationTokenSource.Token).GetAwaiter().GetResult(),
                    _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                LogMessage("Процесс остановлен пользователем");
                MessageBox.Show(@"Процесс остановлен!", @"Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Критическая ошибка: {ex.Message}");
                MessageBox.Show($@"Ошибка: {ex.Message}", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                StopProcess();
            }
        }

        private void StopProcess()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            
            // Останавливаем таймер
            timerElapsed.Stop();

            // Освобождаем сервис токенов
            _tokenService?.Dispose();
            _tokenService = null;

            // Отменяем все активные загрузки в слотах
            lock (_slotsLock)
            {
                foreach (var slot in _uploadSlots)
                {
                    slot.CancellationTokenSource?.Cancel();
                    slot.IsBusy = false;
                }
            }

            // Очищаем слоты
            ClearUploadSlots();

            // Обновляем кнопку
            UpdateStartStopButton(false);

            // Разблокируем форму
            SetFormEnabled(true);

            LogMessage("Процесс остановлен");
        }

        private async Task RunUploadProcess(CancellationToken cancellationToken = default)
        {
            // Проверяем отмену в начале
            cancellationToken.ThrowIfCancellationRequested();

            // Инициализируем сервис токенов
            _tokenService = new TokenService(async () => await GetAccessToken(cancellationToken));

            var allFiles = GetAllFiles(tb_SourceFolder.Text);
            cancellationToken.ThrowIfCancellationRequested();

            var filesToUpload = FilterFiles(allFiles).ToList();

            LogMessage($"Найдено файлов: {allFiles.Length}. После фильтрации: {filesToUpload.Count}");
            Console.WriteLine($@"Найдено файлов для загрузки: {filesToUpload.Count}");

            if (filesToUpload.Count == 0)
            {
                Console.WriteLine(@"Нет файлов для загрузки.");
                return;
            }

            // Создаем HttpClient с обработчиком автоматического обновления токена
            using var tokenHandler = new TokenService.RefreshTokenHandler(_tokenService, LogMessage);
            using var httpClient = new HttpClient(tokenHandler);
            httpClient.Timeout = TimeSpan.FromHours(2);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            var successCount = 0;
            var errorCount   = 0;
            var skippedCount = 0;

            // Инициализируем семафор для ограничения параллелизма
            _uploadSemaphore = new SemaphoreSlim(Settings.MaxParallelUploads, Settings.MaxParallelUploads);

            // Создаем список задач для всех файлов
            var uploadTasks = new List<Task<(bool success, long fileSize)>>();

            for (var i = 0; i < filesToUpload.Count; i++)
            {
                var index = i;
                var task = Task.Run(async () =>
                {
                    await _uploadSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await ProcessFileWithSlot(httpClient, filesToUpload[index], index, filesToUpload.Count, 
                            ref successCount, ref errorCount, ref skippedCount, cancellationToken);
                    }
                    finally
                    {
                        _uploadSemaphore.Release();
                    }
                }, cancellationToken);
                
                uploadTasks.Add(task);
            }

            // Ждем завершения всех задач
            var results = await Task.WhenAll(uploadTasks);

            // Подсчитываем результаты
            foreach (var (success, fileSize) in results)
            {
                if (success && fileSize > 0)
                    _totalBytesUploaded += fileSize;
            }

            // Проверяем отмену перед обработкой пустых папок
            cancellationToken.ThrowIfCancellationRequested();

            // Обработка пустых папок
            FindEmptyFolders(tb_SourceFolder.Text, cancellationToken);
            await CreateEmptyFoldersInApi(httpClient, tb_SourceFolder.Text, _destinationUrl, cancellationToken);

            // Финальный отчет
            var operationEndTime = DateTime.Now;
            var operationDuration = operationEndTime - _operationStartTime;
            
            // Останавливаем таймер перед показом сообщения
            timerElapsed.Stop();
            
            // Рассчитываем среднюю скорость загрузки
            var avgSpeedMbps = operationDuration.TotalSeconds > 0 
                ? _totalBytesUploaded / operationDuration.TotalSeconds / 1024 / 1024 
                : 0;
            
            Console.WriteLine($@"Загрузка завершена!");
            Console.WriteLine($@"Время начала: {_operationStartTime:HH:mm:ss}");
            Console.WriteLine($@"Время окончания: {operationEndTime:HH:mm:ss}");
            Console.WriteLine($@"Продолжительность: {operationDuration.Hours:D2}:{operationDuration.Minutes:D2}:{operationDuration.Seconds:D2}");
            Console.WriteLine($@"Средняя скорость: {avgSpeedMbps:F2} MB/s");
            Console.WriteLine($@"Успешно: {successCount}");
            Console.WriteLine($@"С ошибками: {errorCount}");
            Console.WriteLine($@"Пропущено: {skippedCount}");
            LogMessage($"Загрузка завершена. Время начала: {_operationStartTime:HH:mm:ss}, Время окончания: {operationEndTime:HH:mm:ss}, Продолжительность: {operationDuration.Hours:D2}:{operationDuration.Minutes:D2}:{operationDuration.Seconds:D2}, Средняя скорость: {avgSpeedMbps:F2} MB/s. Успешно: {successCount}, С ошибками: {errorCount}, Пропущено: {skippedCount}");

            MessageBox.Show($"""
                             Обработка файлов завершена.
                             Время начала: {_operationStartTime:HH:mm:ss}
                             Время окончания: {operationEndTime:HH:mm:ss}
                             Продолжительность: {operationDuration.Hours:D2}:{operationDuration.Minutes:D2}:{operationDuration.Seconds:D2}
                             Средняя скорость: {avgSpeedMbps:F2} MB/s
                             Успешно загружено: {successCount}
                             С ошибками: {errorCount}
                             Пропущено (уже существуют): {skippedCount}
                             """, @"Результат", MessageBoxButtons.OK, MessageBoxIcon.Information
            );
        }

        private static string[] GetAllFiles(string folderPath)
        {
            try
            {
                return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при получении файлов из {folderPath}: {ex.Message}");
                throw;
            }
        }

        private static IEnumerable<string> FilterFiles(string[] files)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // Проверяем, не попадает ли файл под исключения
                var isExcluded = ExcludePatterns.Any(pattern =>
                    MatchesPattern(fileName, pattern));

                if (!isExcluded)
                {
                    yield return file;
                }
            }
        }

        private static bool MatchesPattern(string fileName, string pattern)
        {
            // Простая реализация шаблонов с *
            if (pattern.StartsWith('*') && pattern.EndsWith('*'))
            {
                return fileName.Contains(pattern.Trim('*'));
            }

            if (pattern.StartsWith('*'))
            {
                return fileName.EndsWith(pattern.Trim('*'));
            }

            if (pattern.EndsWith('*'))
            {
                return fileName.StartsWith(pattern.Trim('*'));
            }

            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelativePath(string sourceFolder, string fullPath)
        {
            var baseUri = new Uri(sourceFolder + Path.DirectorySeparatorChar);
            var fileUri = new Uri(fullPath);

            return Uri.UnescapeDataString(
                baseUri.MakeRelativeUri(fileUri).ToString()
            ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private async Task<bool> UploadFileWithRetry(
            HttpClient httpClient,
            string filePath,
            string relativePath,
            int maxRetries,
            string destinationUrl,
            CancellationToken cancellationToken = default,
            FileUploadSlot? slot = null)
        {
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Проверяем доступность файла
                    if (!IsFileAccessible(filePath))
                    {
                        LogMessage($"Попытка {attempt}: Файл недоступен для чтения - {filePath}");
                        if (attempt == maxRetries) return false;

                        await Task.Delay(1000 * attempt, cancellationToken);
                        continue;
                    }

                    if (await UploadFile(httpClient, filePath, relativePath, destinationUrl, 
                            slot != null ? (bytes, total) => UpdateSlotProgress(slot, bytes, total) : UpdateFileUploadProgress, 
                            cancellationToken))
                    {
                        return true;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogMessage($"Попытка {attempt} ошибка для {relativePath}: {ex.Message}");
                }

                if (attempt >= maxRetries)
                    continue;

                LogMessage($"Повторная попытка через {attempt} секунд...");
                await Task.Delay(1000 * attempt, cancellationToken);
            }

            return false;
        }

        private static bool IsFileAccessible(string filePath)
        {
            try
            {
                using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return fileStream.CanRead;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> UploadFile(
            HttpClient httpClient,
            string filePath,
            string relativePath,
            string destinationUrl,
            Action<long, long>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var fileInfo   = new FileInfo(filePath);
            var totalBytes = fileInfo.Length;

            await using var fileStream = File.OpenRead(filePath);

            // Получаем MIME-тип
            var mimeType = GetMimeType(filePath);

            // Кодируем путь для URL
            var encodedPath = Uri.EscapeDataString(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
            var requestUrl  = $"{_putObjectUrl}?name={destinationUrl}{encodedPath}";

            // Создаем multipart form-data
            using var formData = new MultipartFormDataContent();

            // Создаем кастомный StreamContent для отслеживания прогресса
            var progressStreamContent = new ProgressStreamContent(fileStream, progressCallback, totalBytes, cancellationToken);
            progressStreamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            // Добавляем файл в form-data с правильными заголовками
            formData.Add(progressStreamContent, "File", Path.GetFileName(filePath));

            // Устанавливаем заголовки
            var request = new HttpRequestMessage(HttpMethod.Put, requestUrl)
            {
                Content = formData
            };

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromHours(2));

            var response = await httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            LogMessage($"HTTP ошибка {(int)response.StatusCode}: {errorContent}");
            return false;
        }

        private static string GetMimeType(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                // Простой словарь распространенных MIME-типов
                var mimeTypes = new Dictionary<string, string>
                {
                    { ".txt", "text/plain" },
                    { ".pdf", "application/pdf" },
                    { ".doc", "application/msword" },
                    { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                    { ".xls", "application/vnd.ms-excel" },
                    { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                    { ".ppt", "application/vnd.ms-powerpoint" },
                    { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                    { ".jpg", "image/jpeg" },
                    { ".jpeg", "image/jpeg" },
                    { ".png", "image/png" },
                    { ".gif", "image/gif" },
                    { ".zip", "application/zip" },
                    { ".rar", "application/x-rar-compressed" }
                };

                if (mimeTypes.TryGetValue(extension, out var mimeType))
                {
                    return mimeType;
                }

                // Пытаемся получить из реестра Windows
                const string defaultMime = "application/octet-stream";
                try
                {
                    var regKey = Registry.ClassesRoot.OpenSubKey(extension);
                    if (regKey?.GetValue("Content Type") is string registryMime)
                    {
                        return registryMime;
                    }
                }
                catch
                {
                    // Игнорируем ошибки реестра
                }

                return defaultMime;
            }
            catch
            {
                return "application/octet-stream";
            }
        }

        private static string GetFileSizeReadable(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var bytes    = fileInfo.Length;

                string[] sizes = ["B", "KB", "MB", "GB", "TB"];
                var      order = 0;
                double   size  = bytes;

                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size = size / 1024;
                }

                return $"{size:0.##} {sizes[order]}";
            }
            catch
            {
                return "Unknown size";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            var      order = 0;
            double   size  = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private static long CalculateTotalFolderSize(string folderPath)
        {
            try
            {
                long totalSize = 0;
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // Игнорируем файлы, к которым нет доступа
                    }
                }
                
                return totalSize;
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateProgressBar(int current, int total, int success, int errors, int skipped = 0)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, int, int, int, int>(UpdateProgressBar), current, total, success, errors, skipped);
                return;
            }

            var percentage = (double)current / total * 100;

            // Обновляем прогресс-бар
            progressBar1.Value = Math.Min((int)((double)current / total * 100), progressBar1.Maximum);

            // Обновляем текстовые поля с учетом пропущенных файлов
            label3.Text = $@"{current}/{total} файлов | {FormatFileSize(_totalBytesUploaded)}/{FormatFileSize(_totalSourceFolderSize)} | Успешно: {success} | Ошибки: {errors} | Пропущено: {skipped}";

            // Принудительно обновляем интерфейс
            progressBar1.Refresh();
            label3.Refresh();

            Console.Write($@"\rПрогресс: [{
                new string('=', (int)(percentage / 2))}{
                    new string(' ', 50 - (int)(percentage / 2))}] {percentage:0.0}% | {current}/{total} | Успешно: {success} | Ошибки: {errors} | Пропущено: {skipped}");
        }

        private void UpdateFileUploadProgress(long bytesRead, long totalBytes)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<long, long>(UpdateFileUploadProgress), bytesRead, totalBytes);
                return;
            }

            if (totalBytes <= 0)
                return;

            var percentage = (int)((double)bytesRead / totalBytes * 100);
            progressBar2.Value = Math.Min(percentage, progressBar2.Maximum);

            // Обновляем информацию о прогрессе
            var currentMb = (double)bytesRead / (1024 * 1024);
            var totalMb   = (double)totalBytes / (1024 * 1024);
            label15.Text = $@"Прогресс текущего файла: {currentMb:0.0} / {totalMb:0.0} MB ({percentage}%)";

            progressBar2.Refresh();
            label15.Refresh();
        }

        private void ResetFileProgress()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(ResetFileProgress);
                return;
            }

            progressBar2.Value = 0;
            label15.Text       = @"Прогресс текущего файла:";
        }

        /// <summary>
        /// Инициализация слотов для параллельной загрузки
        /// </summary>
        private void InitializeUploadSlots()
        {
            _uploadSlots = new List<FileUploadSlot>();
            
            // Очищаем панель от старых контролов
            pnlFileSlots.Controls.Clear();
            
            // Создаем слоты согласно настройке MaxParallelUploads
            for (int i = 0; i < Settings.MaxParallelUploads; i++)
            {
                // Создаем прогресс-бар для слота
                var progressBar = new ProgressBar
                {
                    Name = $"pbSlot{i}",
                    Size = new System.Drawing.Size(380, 20),
                    Maximum = 100,
                    Value = 0,
                    Margin = new Padding(0, 2, 0, 2)
                };
                
                // Создаем метку для информации о файле
                var labelInfo = new Label
                {
                    Name = $"lblSlot{i}",
                    Text = $"Слот {i + 1}: Ожидание...",
                    AutoSize = false,
                    Size = new System.Drawing.Size(380, 15),
                    Font = new System.Drawing.Font("Segoe UI", 8F),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 0, 0, 5)
                };
                
                // Добавляем контролы в панель
                pnlFileSlots.Controls.Add(labelInfo);
                pnlFileSlots.Controls.Add(progressBar);
                
                // Создаем объект слота
                var slot = new FileUploadSlot(i, progressBar, labelInfo);
                _uploadSlots.Add(slot);
            }
        }
        
        /// <summary>
        /// Очистка слотов после завершения загрузки
        /// </summary>
        private void ClearUploadSlots()
        {
            lock (_slotsLock)
            {
                foreach (var slot in _uploadSlots)
                {
                    slot.Reset();
                    slot.IsBusy = false;
                    slot.CancellationTokenSource?.Dispose();
                    slot.CancellationTokenSource = null;
                }
            }
        }
        
        /// <summary>
        /// Получение свободного слота для загрузки
        /// </summary>
        private FileUploadSlot? GetAvailableSlot()
        {
            lock (_slotsLock)
            {
                foreach (var slot in _uploadSlots)
                {
                    if (!slot.IsBusy)
                    {
                        slot.IsBusy = true;
                        slot.CancellationTokenSource = new CancellationTokenSource();
                        return slot;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Обновление прогресса для конкретного слота
        /// </summary>
        private void UpdateSlotProgress(FileUploadSlot slot, long bytesRead, long totalBytes)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<FileUploadSlot, long, long>(UpdateSlotProgress), slot, bytesRead, totalBytes);
                return;
            }
            
            slot.UpdateProgress(bytesRead, totalBytes);
        }
        
        /// <summary>
        /// Обработка файла с использованием слота
        /// </summary>
        private async Task<(bool success, long fileSize)> ProcessFileWithSlot(
            HttpClient httpClient, 
            string filePath, 
            int fileIndex, 
            int totalFiles,
            ref int successCount, 
            ref int errorCount, 
            ref int skippedCount, 
            CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            var relativePath = GetRelativePath(tb_SourceFolder.Text, filePath);
            var fileName = Path.GetFileName(filePath);
            
            // Ждем доступный слот
            FileUploadSlot? slot = null;
            while (slot == null && !cancellationToken.IsCancellationRequested)
            {
                slot = GetAvailableSlot();
                if (slot == null)
                    await Task.Delay(50, cancellationToken);
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                return (false, 0);
            }
            
            try
            {
                // Устанавливаем файл в слот
                slot.SetFile(fileName, fileInfo.Length);
                
                // Обновляем информацию о текущем файле
                var folderPath = Path.GetDirectoryName(relativePath) ?? "";
                UpdateFileInfo(folderPath, fileName);
                
                // Проверяем существование файла если включена настройка
                bool fileExists = false;
                if (Settings.CheckFileExistsBeforeUpload)
                {
                    fileExists = await CheckFileExists(httpClient, relativePath, _destinationUrl, cancellationToken);
                }
                
                if (fileExists)
                {
                    LogMessage($"Файл уже существует: {relativePath}");
                    skippedCount++;
                    UpdateProgressBar(fileIndex + 1, totalFiles, successCount, errorCount, skippedCount);
                    return (true, 0);
                }
                
                // Загружаем файл с повторными попытками
                var uploadSuccess = await UploadFileWithRetry(
                    httpClient, 
                    filePath, 
                    relativePath, 
                    3, 
                    _destinationUrl, 
                    cancellationToken,
                    slot);
                
                if (uploadSuccess)
                {
                    successCount++;
                    LogMessage($"Успешно загружен: {relativePath}");
                }
                else
                {
                    errorCount++;
                    LogMessage($"Ошибка загрузки: {relativePath}");
                    
                    // Обновляем метку слота об ошибке
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => slot.LabelInfo.Text = $"{slot.FileName[..Math.Min(12, slot.FileName.Length)]}...: ОШИБКА!");
                    }
                    else
                    {
                        slot.LabelInfo.Text = $"{slot.FileName[..Math.Min(12, slot.FileName.Length)]}...: ОШИБКА!";
                    }
                }
                
                UpdateProgressBar(fileIndex + 1, totalFiles, successCount, errorCount, skippedCount);
                
                return (uploadSuccess, uploadSuccess ? fileInfo.Length : 0);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogMessage($"Загрузка файла {fileName} отменена");
                throw;
            }
            catch (Exception ex)
            {
                errorCount++;
                LogMessage($"Исключение при загрузке {fileName}: {ex.Message}");
                UpdateProgressBar(fileIndex + 1, totalFiles, successCount, errorCount, skippedCount);
                return (false, 0);
            }
            finally
            {
                // Освобождаем слот
                slot.IsBusy = false;
                slot.CancellationTokenSource?.Cancel();
                slot.CancellationTokenSource?.Dispose();
                slot.CancellationTokenSource = null;
            }
        }

        private static void LogMessage(string message)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            _logWriter.WriteLine(logEntry);
            _logWriter.Flush();
        }

        private static void FindEmptyFolders(string folderPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var directories = Directory.GetDirectories(folderPath);

                foreach (var directory in directories)
                {
                    // Проверяем отмену для каждой папки (для очень больших структур)
                    cancellationToken.ThrowIfCancellationRequested();

                    // Рекурсивно проверяем вложенные папки
                    FindEmptyFolders(directory, cancellationToken);

                    // Проверяем, пустая ли текущая папка
                    var files   = Directory.GetFiles(directory);
                    var subDirs = Directory.GetDirectories(directory);

                    if (files.Length != 0 || subDirs.Length != 0)
                        continue;

                    EmptyFolders.Add(directory);
                    LogMessage($"Найдена пустая папка: {directory}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при поиске пустых папок в {folderPath}: {ex.Message}");
            }
        }

        private async Task<string> GetAccessToken(CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = new HttpClient();

                // Устанавливаем заголовки
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FileUploader/1.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

                // Формируем тело запроса
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("username", tb_UserName.Text),
                    new("password", tb_Password.Text),
                    new("client_id", tb_ClientId.Text),
                    new("client_secret", tb_ClientSecret.Text),
                    new("grant_type", tb_GrantType.Text)
                };

                var content = new FormUrlEncodedContent(formData);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1)); // Таймаут 1 минута для получения токена

                // Отправляем POST запрос
                var response = await httpClient.PostAsync(_tokenUrl, content, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
                    var tokenResponse   = Deserialize<TokenResponse>(responseContent);

                    if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                        throw new Exception("Access token не найден в ответе");

                    // Устанавливаем время истечения токена в сервисе
                    if (tokenResponse.ExpiresIn > 0 && _tokenService != null)
                    {
                        _tokenService.SetTokenExpiration(tokenResponse.ExpiresIn);
                    }

                    LogMessage($"Access token успешно получен. Время жизни: {tokenResponse.ExpiresIn} сек.");
                    return tokenResponse.AccessToken;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                throw new Exception($"HTTP ошибка: {(int)response.StatusCode} - {errorContent}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogMessage("Получение access token отменено");
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при получении access token: {ex.Message}");
                throw;
            }
        }

        private static async Task<bool> CreateFolderInApi(
            HttpClient httpClient,
            string folderPath,
            string sourceFolder,
            string destinationUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var relativePath = GetRelativePath(sourceFolder, folderPath);

                // Кодируем путь для URL (добавляем / в конце для папки)
                var encodedPath = Uri.EscapeDataString(relativePath.Replace(Path.DirectorySeparatorChar, '/') + "/");
                var requestUrl  = $"{_createFolderUrl}?Name={destinationUrl}{encodedPath}";

                // Создаем POST запрос с пустым телом
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StringContent("")
                };

                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1)); // Таймаут 1 минута для создания папки

                var response = await httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    LogMessage($"Папка создана в API: {relativePath}");
                    return true;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Обрабатываем случай когда папка уже существует
                    var errorContent1 = await response.Content.ReadAsStringAsync(cts.Token);

                    try
                    {
                        // Пытаемся десериализовать ответ для проверки ошибки
                        var errorResponse = Deserialize<FolderErrorResponse>(errorContent1);
                        if (errorResponse?.Error == "FolderExists")
                        {
                            LogMessage($"Папка уже существует в API: {relativePath}");
                            return true; // Считаем успехом, так как папка уже есть
                        }

                        LogMessage($"Ошибка 400 при создании папки {relativePath}: {errorResponse?.Message}");
                        return false;
                    }
                    catch
                    {
                        // Если не удалось распарсить JSON, логируем сырой ответ
                        LogMessage($"Ошибка 400 при создании папки {relativePath}: {errorContent1}");
                        return false;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                LogMessage($"Ошибка при создании папки {relativePath}: HTTP {(int)response.StatusCode} - {errorContent}");
                return false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogMessage($"Создание папки {folderPath} отменено");
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"Исключение при создании папки {folderPath}: {ex.Message}");
                return false;
            }
        }

        private static async Task CreateEmptyFoldersInApi(
            HttpClient httpClient,
            string sourceFolder,
            string destinationUrl,
            CancellationToken cancellationToken = default)
        {
            if (EmptyFolders.Count == 0)
            {
                Console.WriteLine(@"Нет пустых папок для создания в API.");
                return;
            }

            Console.WriteLine($@"Создание {EmptyFolders.Count} пустых папок в API...");

            var successCount = 0;
            var errorCount   = 0;

            for (var i = 0; i < EmptyFolders.Count; i++)
            {
                // Проверяем отмену перед каждой папкой
                cancellationToken.ThrowIfCancellationRequested();

                var folderPath   = EmptyFolders[i];
                var relativePath = GetRelativePath(sourceFolder, folderPath);

                Console.WriteLine($@"[{i + 1}/{EmptyFolders.Count}] Создание папки: {relativePath}");

                // Пытаемся создать папку с повторными попытками
                var success = false;
                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    success = await CreateFolderInApi(httpClient, folderPath, sourceFolder, destinationUrl, cancellationToken);
                    if (success)
                    {
                        successCount++;
                        break;
                    }

                    if (attempt >= 3)
                        continue;

                    LogMessage($"Повторная попытка {attempt} для папки {relativePath}");
                    await Task.Delay(1000 * attempt, cancellationToken);
                }

                if (!success)
                {
                    errorCount++;
                    LogMessage($"Не удалось создать папку после 3 попыток: {relativePath}");
                }

                // Обновляем прогресс
                UpdateFolderProgressBar(i + 1, EmptyFolders.Count, successCount, errorCount);
            }

            Console.WriteLine($@"Создание папок завершено: Успешно {successCount}, Ошибки {errorCount}");
            LogMessage($"Создание пустых папок в API завершено: Успешно {successCount}, Ошибки {errorCount}");
        }

        private static void UpdateFolderProgressBar(int current, int total, int success, int errors, int skipped = 0)
        {
            var percentage = (double)current / total * 100;
            Console.Write($@"\rПрогресс создания папок: [{
                new string('=', (int)(percentage / 2))}{
                    new string(' ', 50 - (int)(percentage / 2))}] {percentage:0.0}% | {current}/{total} | Успешно: {success} | Ошибки: {errors} | Пропущено: {skipped}");
        }

        private void SetFormEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetFormEnabled), enabled);
                return;
            }

            // Блокируем/разблокируем все основные контролы
            btChoiceFolder.Enabled = enabled;
            btGetFileList.Enabled  = enabled;
            btSaveSettings.Enabled = enabled;

            // Блокируем поля настроек
            tb_BaseUrlApi.Enabled      = enabled;
            tb_BucketName.Enabled      = enabled;
            tb_BaseUrlKeycloak.Enabled = enabled;
            tb_Realm.Enabled           = enabled;
            tb_UserName.Enabled        = enabled;
            tb_Password.Enabled        = enabled;
            tb_ClientId.Enabled        = enabled;
            tb_GrantType.Enabled       = enabled;
            tb_ClientSecret.Enabled    = enabled;
            tb_Destination.Enabled     = enabled;

            // Курсор ожидания
            this.Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;

            // Обновляем интерфейс
            FormsApplication.DoEvents();
        }

        private void UpdateFileInfo(string currentFolder, string currentFile)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string>(UpdateFileInfo), currentFolder, currentFile);
                return;
            }

            tbcurrentFolder.Text = currentFolder;
            tbCurentFile.Text    = currentFile;

            // Принудительное обновление
            tbcurrentFolder.Refresh();
            tbCurentFile.Refresh();
        }

        private static async Task<bool> CheckFileExists(
            HttpClient httpClient,
            string relativePath,
            string destinationUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Кодируем путь для URL
                var encodedPath = Uri.EscapeDataString(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
                var requestUrl  = $"{_checkFileExistsUrl}?name={destinationUrl}{encodedPath}";

                // Создаем GET запрос
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1)); // Таймаут 1 минута

                var response = await httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
                    var existsResponse  = Deserialize<FileExistsResponse>(responseContent);
                    if (existsResponse?.IsSuccess == true)
                    {
                        return existsResponse.Exists;
                    }

                    LogMessage($"Ошибка при проверке существования файла {relativePath}: {existsResponse?.Error}");
                    return false; // В случае ошибки считаем что файла нет и будем загружать
                }

                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                LogMessage($"HTTP ошибка при проверке файла {relativePath}: {(int)response.StatusCode} - {errorContent}");
                return false; // В случае ошибки считаем что файла нет
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogMessage($"Проверка существования файла {relativePath} отменена");
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"Исключение при проверке существования файла {relativePath}: {ex.Message}");
                return false; // В случае исключения считаем что файла нет
            }
        }

        private void timerElapsed_Tick(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, EventArgs>(timerElapsed_Tick), sender, e);
                return;
            }

            var elapsed = DateTime.Now - _operationStartTime;
            labelElapsedTime.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
    }
}