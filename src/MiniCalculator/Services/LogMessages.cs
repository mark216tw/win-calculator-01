using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "讀取 JSON 檔失敗：{Path}")]
    public static partial void JsonReadFailed(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "序列化資料失敗：{Path}")]
    public static partial void JsonSerializeFailed(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "寫入 JSON 檔失敗：{Path}")]
    public static partial void JsonWriteFailed(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "運算式求值發生未預期的例外：{Expression}")]
    public static partial void EvaluationFailed(ILogger logger, string expression, Exception exception);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "套用主題失敗：{RelativePath}")]
    public static partial void ThemeApplyFailed(ILogger logger, string relativePath, Exception exception);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "建立單一實例 Mutex 失敗")]
    public static partial void MutexCreateFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Error, Message = "還原既有視窗失敗")]
    public static partial void WindowActivationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Warning, Message = "無法通知既有實例")]
    public static partial void SignalExistingFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Error, Message = "未處理的例外")]
    public static partial void UnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "複製結果失敗")]
    public static partial void ClipboardCopyFailed(ILogger logger, Exception exception);
}
