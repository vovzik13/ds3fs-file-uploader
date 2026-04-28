namespace Ds3fsFileUploader;

/// <summary>
/// Представляет слот для загрузки файла с собственным прогресс-баром
/// </summary>
public class FileUploadSlot
{
    public int SlotIndex { get; }
    public ProgressBar ProgressBar { get; set; } = null!;
    public Label LabelInfo { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long BytesUploaded { get; set; }
    public bool IsBusy { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public FileUploadSlot(int slotIndex, ProgressBar progressBar, Label labelInfo)
    {
        SlotIndex = slotIndex;
        ProgressBar = progressBar;
        LabelInfo = labelInfo;
    }

    public void UpdateProgress(long bytesRead, long totalBytes)
    {
        // Проверяем, требуется ли инвокация для ProgressBar (он создан в UI потоке)
        if (ProgressBar.InvokeRequired)
        {
            ProgressBar.Invoke(new Action<long, long>(UpdateProgress), bytesRead, totalBytes);
            return;
        }
        
        BytesUploaded = bytesRead;
        TotalBytes = totalBytes;

        if (totalBytes <= 0)
            return;

        var percentage = (int)((double)bytesRead / totalBytes * 100);
        ProgressBar.Value = Math.Min(percentage, ProgressBar.Maximum);

        var currentMb = (double)bytesRead / (1024 * 1024);
        var totalMb = (double)totalBytes / (1024 * 1024);
        
        // Обрезаем имя файла до 15 символов
        var displayName = FileName.Length > 15 ? FileName[..12] + "..." : FileName;
        LabelInfo.Text = $"{displayName}: {currentMb:0.0} / {totalMb:0.0} MB ({percentage}%)";
    }

    public void Reset()
    {
        // Проверяем, требуется ли инвокация для контролов (они созданы в UI потоке)
        if (ProgressBar.InvokeRequired)
        {
            ProgressBar.Invoke(new Action(Reset));
            return;
        }
        
        BytesUploaded = 0;
        TotalBytes = 0;
        FileName = string.Empty;
        ProgressBar.Value = 0;
        LabelInfo.Text = $"Слот {SlotIndex + 1}: Ожидание...";
    }

    public void SetFile(string fileName, long fileSize)
    {
        // Проверяем, требуется ли инвокация для контролов (они созданы в UI потоке)
        if (ProgressBar.InvokeRequired)
        {
            ProgressBar.Invoke(new Action<string, long>(SetFile), fileName, fileSize);
            return;
        }
        
        FileName = fileName;
        TotalBytes = fileSize;
        BytesUploaded = 0;
        
        var displayName = fileName.Length > 15 ? fileName[..12] + "..." : fileName;
        LabelInfo.Text = $"{displayName}: 0.0 / {fileSize / (1024.0 * 1024.0):0.0} MB (0%)";
        ProgressBar.Value = 0;
    }
}
