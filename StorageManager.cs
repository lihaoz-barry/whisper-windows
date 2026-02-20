using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace whisper_windows
{
    public class StorageManager
    {
        private const int MaxHistoryRecordings = 3;
        private static readonly string AppName = "WhisperWindows";
        private static readonly string RecordingsFolderName = "Recordings";

        private readonly string _recordingsPath;

        public StorageManager()
        {
            _recordingsPath = InitializeRecordingsPath();
        }

        public string RecordingsPath => _recordingsPath;

        public string GetNewRecordingPath()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{timestamp}_original.wav";
            return Path.Combine(_recordingsPath, fileName);
        }

        public List<string> GetRecordingHistory()
        {
            if (!Directory.Exists(_recordingsPath))
                return new List<string>();

            return Directory.GetFiles(_recordingsPath, "*_original.wav")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();
        }

        public void CleanupOldRecordings()
        {
            var recordings = GetRecordingHistory();
            if (recordings.Count <= MaxHistoryRecordings)
                return;

            foreach (var old in recordings.Skip(MaxHistoryRecordings))
            {
                try
                {
                    File.Delete(old);

                    // Also delete associated .txt transcription file
                    string txtPath = Path.ChangeExtension(old, ".txt");
                    if (File.Exists(txtPath))
                        File.Delete(txtPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to cleanup {old}: {ex.Message}");
                }
            }
        }

        public void SaveTranscriptionResult(string wavPath, string text)
        {
            try
            {
                string txtPath = Path.ChangeExtension(wavPath, ".txt");
                File.WriteAllText(txtPath, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to save transcription: {ex.Message}");
            }
        }

        private string InitializeRecordingsPath()
        {
            // Primary: %APPDATA%\WhisperWindows\Recordings
            string primary = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName, RecordingsFolderName);

            if (TryEnsureDirectory(primary))
                return primary;

            // Fallback: %TEMP%\WhisperWindows\Recordings
            string fallback = Path.Combine(
                Path.GetTempPath(), AppName, RecordingsFolderName);

            if (TryEnsureDirectory(fallback))
                return fallback;

            throw new IOException("No suitable storage location found.");
        }

        private static bool TryEnsureDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                // Test write permission
                string testFile = Path.Combine(path, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
