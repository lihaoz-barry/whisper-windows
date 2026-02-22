using System;

namespace whisper_windows
{
    public class RecordingHistoryItem
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string TranscriptionText { get; set; } = string.Empty;
        public bool TranscriptionFailed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
