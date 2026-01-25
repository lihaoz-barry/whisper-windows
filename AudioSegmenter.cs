using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace whisper_windows
{
    public class AudioSegment
    {
        public int SegmentNumber { get; set; }
        public int TotalSegments { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class AudioSegmenter
    {
        private const int MAX_SEGMENT_DURATION_SECONDS = 180; // 3 minutes

        /// <summary>
        /// Gets the duration of an audio file
        /// </summary>
        public static TimeSpan GetAudioDuration(string filePath)
        {
            try
            {
                using (var reader = new WaveFileReader(filePath))
                {
                    return reader.TotalTime;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read audio duration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Determines if audio needs to be segmented
        /// </summary>
        public static bool NeedsSegmentation(string filePath)
        {
            var duration = GetAudioDuration(filePath);
            return duration.TotalSeconds > MAX_SEGMENT_DURATION_SECONDS;
        }

        /// <summary>
        /// Splits an audio file into equal segments, each under 3 minutes
        /// </summary>
        public static List<AudioSegment> SegmentAudio(string inputFilePath, string outputDirectory = null)
        {
            if (outputDirectory == null)
            {
                outputDirectory = Path.GetDirectoryName(inputFilePath);
                // Handle case where file is in current directory
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    outputDirectory = Directory.GetCurrentDirectory();
                }
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var segments = new List<AudioSegment>();
            var totalDuration = GetAudioDuration(inputFilePath);

            // Calculate optimal number of segments
            int segmentCount = (int)Math.Ceiling(totalDuration.TotalSeconds / MAX_SEGMENT_DURATION_SECONDS);

            if (segmentCount <= 1)
            {
                // No segmentation needed
                return segments;
            }

            // Calculate exact segment duration
            double segmentDurationSeconds = totalDuration.TotalSeconds / segmentCount;

            using (var reader = new WaveFileReader(inputFilePath))
            {
                var waveFormat = reader.WaveFormat;

                for (int i = 0; i < segmentCount; i++)
                {
                    long startSample = (long)(i * segmentDurationSeconds * waveFormat.SampleRate);
                    long endSample = i == segmentCount - 1
                        ? reader.SampleCount
                        : (long)((i + 1) * segmentDurationSeconds * waveFormat.SampleRate);

                    long sampleCount = endSample - startSample;

                    string segmentFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_seg_{i + 1}_of_{segmentCount}.wav";
                    string segmentFilePath = Path.Combine(outputDirectory, segmentFileName);

                    ExtractAudioSegment(inputFilePath, segmentFilePath, startSample, sampleCount);

                    var segmentDuration = TimeSpan.FromSeconds((double)sampleCount / waveFormat.SampleRate);

                    segments.Add(new AudioSegment
                    {
                        SegmentNumber = i + 1,
                        TotalSegments = segmentCount,
                        FilePath = segmentFilePath,
                        Duration = segmentDuration
                    });
                }
            }

            return segments;
        }

        /// <summary>
        /// Extracts a specific segment from an audio file
        /// </summary>
        private static void ExtractAudioSegment(string inputFilePath, string outputFilePath, long startSample, long sampleCount)
        {
            try
            {
                using (var reader = new WaveFileReader(inputFilePath))
                {
                    var waveFormat = reader.WaveFormat;
                    var bytesPerSample = waveFormat.BitsPerSample / 8 * waveFormat.Channels;
                    long bytesToSkip = startSample * bytesPerSample;

                    using (var writer = new WaveFileWriter(outputFilePath, waveFormat))
                    {
                        // Skip to start position
                        byte[] skipBuffer = new byte[65536];
                        long bytesSkipped = 0;
                        while (bytesSkipped < bytesToSkip)
                        {
                            int toRead = (int)Math.Min(skipBuffer.Length, bytesToSkip - bytesSkipped);
                            int read = reader.Read(skipBuffer, 0, toRead);
                            if (read == 0) break;
                            bytesSkipped += read;
                        }

                        // Read and write segment
                        byte[] buffer = new byte[65536];
                        long bytesWritten = 0;
                        long targetBytes = sampleCount * bytesPerSample;

                        while (bytesWritten < targetBytes)
                        {
                            int toRead = (int)Math.Min(buffer.Length, targetBytes - bytesWritten);
                            int read = reader.Read(buffer, 0, toRead);
                            if (read == 0) break;

                            writer.Write(buffer, 0, read);
                            bytesWritten += read;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract audio segment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleans up temporary segment files
        /// </summary>
        public static void CleanupSegments(List<AudioSegment> segments)
        {
            foreach (var segment in segments)
            {
                try
                {
                    if (File.Exists(segment.FilePath))
                    {
                        File.Delete(segment.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to cleanup segment {segment.FilePath}: {ex.Message}");
                }
            }
        }
    }
}
