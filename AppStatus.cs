using System;
using System.Drawing;

namespace whisper_windows
{
    /// <summary>
    /// Application status states
    /// </summary>
    public enum AppStatus
    {
        /// <summary>Idle - waiting for user action</summary>
        Idle,
        /// <summary>Recording audio from microphone</summary>
        Recording,
        /// <summary>Processing recorded audio (preparing for submission)</summary>
        Processing,
        /// <summary>Sending audio to API</summary>
        Sending,
        /// <summary>Waiting for transcription from API</summary>
        Transcribing,
        /// <summary>Transcription completed successfully</summary>
        Success,
        /// <summary>An error occurred</summary>
        Error
    }

    /// <summary>
    /// Holds display information for each status state
    /// </summary>
    public class StatusInfo
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string TextEn { get; set; }
        public Color Color { get; set; }
        public string DetailMessage { get; set; }

        public StatusInfo(string icon, string text, string textEn, Color color, string detailMessage = "")
        {
            Icon = icon;
            Text = text;
            TextEn = textEn;
            Color = color;
            DetailMessage = detailMessage;
        }
    }

    /// <summary>
    /// Manages application status and provides status information
    /// </summary>
    public class StatusManager
    {
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        private AppStatus _currentStatus = AppStatus.Idle;
        private string _detailMessage = "";
        private int _currentSegment = 0;
        private int _totalSegments = 0;

        public AppStatus CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                if (_currentStatus != value)
                {
                    var oldStatus = _currentStatus;
                    _currentStatus = value;
                    OnStatusChanged(oldStatus, value);
                }
            }
        }

        public string DetailMessage
        {
            get => _detailMessage;
            private set => _detailMessage = value;
        }

        public int CurrentSegment => _currentSegment;
        public int TotalSegments => _totalSegments;

        /// <summary>
        /// Get status display info for a given status
        /// </summary>
        public static StatusInfo GetStatusInfo(AppStatus status)
        {
            return status switch
            {
                AppStatus.Idle => new StatusInfo("⏸", "就绪", "Ready", Color.FromArgb(100, 100, 100)),
                AppStatus.Recording => new StatusInfo("🎤", "录制中", "Recording", Color.FromArgb(220, 53, 69)),
                AppStatus.Processing => new StatusInfo("⚙", "处理中", "Processing", Color.FromArgb(255, 193, 7)),
                AppStatus.Sending => new StatusInfo("📤", "发送中", "Sending", Color.FromArgb(0, 123, 255)),
                AppStatus.Transcribing => new StatusInfo("⏳", "转录中", "Transcribing", Color.FromArgb(111, 66, 193)),
                AppStatus.Success => new StatusInfo("✓", "完成", "Success", Color.FromArgb(40, 167, 69)),
                AppStatus.Error => new StatusInfo("✗", "错误", "Error", Color.FromArgb(220, 53, 69)),
                _ => new StatusInfo("?", "未知", "Unknown", Color.Gray)
            };
        }

        /// <summary>
        /// Get the current status info with detail message
        /// </summary>
        public StatusInfo GetCurrentStatusInfo()
        {
            var info = GetStatusInfo(CurrentStatus);
            info.DetailMessage = _detailMessage;
            return info;
        }

        /// <summary>
        /// Set status to Idle
        /// </summary>
        public void SetIdle()
        {
            _detailMessage = "";
            _currentSegment = 0;
            _totalSegments = 0;
            CurrentStatus = AppStatus.Idle;
        }

        /// <summary>
        /// Set status to Recording
        /// </summary>
        public void SetRecording()
        {
            _detailMessage = "按 Ctrl+M 停止录制";
            CurrentStatus = AppStatus.Recording;
        }

        /// <summary>
        /// Set status to Processing
        /// </summary>
        public void SetProcessing(string detail = "")
        {
            _detailMessage = string.IsNullOrEmpty(detail) ? "正在处理音频..." : detail;
            CurrentStatus = AppStatus.Processing;
        }

        /// <summary>
        /// Set status to Sending with optional file size info
        /// </summary>
        public void SetSending(long? fileSizeBytes = null)
        {
            if (fileSizeBytes.HasValue)
            {
                var sizeMB = fileSizeBytes.Value / (1024.0 * 1024.0);
                _detailMessage = $"正在发送音频 ({sizeMB:F1} MB)...";
            }
            else
            {
                _detailMessage = "正在发送音频...";
            }
            CurrentStatus = AppStatus.Sending;
        }

        /// <summary>
        /// Set status to Transcribing
        /// </summary>
        public void SetTranscribing()
        {
            _detailMessage = "等待 API 响应...";
            CurrentStatus = AppStatus.Transcribing;
        }

        /// <summary>
        /// Set status to Transcribing with segment progress
        /// </summary>
        public void SetTranscribingSegment(int current, int total)
        {
            _currentSegment = current;
            _totalSegments = total;
            _detailMessage = $"正在转录第 {current}/{total} 段...";
            CurrentStatus = AppStatus.Transcribing;
        }

        /// <summary>
        /// Set status to Success
        /// </summary>
        public void SetSuccess(string detail = "")
        {
            _detailMessage = string.IsNullOrEmpty(detail) ? "转录完成，已复制到剪贴板" : detail;
            CurrentStatus = AppStatus.Success;
        }

        /// <summary>
        /// Set status to Error
        /// </summary>
        public void SetError(string errorMessage)
        {
            _detailMessage = errorMessage;
            CurrentStatus = AppStatus.Error;
        }

        protected virtual void OnStatusChanged(AppStatus oldStatus, AppStatus newStatus)
        {
            StatusChanged?.Invoke(this, new StatusChangedEventArgs(oldStatus, newStatus, _detailMessage));
        }
    }

    /// <summary>
    /// Event args for status change events
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        public AppStatus OldStatus { get; }
        public AppStatus NewStatus { get; }
        public string DetailMessage { get; }

        public StatusChangedEventArgs(AppStatus oldStatus, AppStatus newStatus, string detailMessage)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            DetailMessage = detailMessage;
        }
    }
}
