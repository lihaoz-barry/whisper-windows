using System;

namespace whisper_windows
{
    public class RecordingHistoryItem
    {
        public string FilePath { get; set; }
        public DateTime RecordedAt { get; set; }
        public string TranscriptionText { get; set; }
        public bool TranscriptionFailed { get; set; }
        public string ErrorMessage { get; set; }
    }
}
