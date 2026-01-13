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

                    // Seek to start position
                    reader.CurrentTime = TimeSpan.FromSeconds((double)startSample / waveFormat.SampleRate);

                    using (var writer = new WaveFileWriter(outputFilePath, waveFormat))
                    {
                        int bufferSize = waveFormat.SampleRate * bytesPerSample; // 1 second buffer
                        byte[] buffer = new byte[bufferSize];
                        long samplesRead = 0;

                        while (samplesRead < sampleCount)
                        {
                            long samplesToRead = Math.Min(bufferSize / bytesPerSample, sampleCount - samplesRead);
                            int bytesToRead = (int)(samplesToRead * bytesPerSample);

                            int bytesRead = reader.Read(buffer, 0, bytesToRead);
                            if (bytesRead == 0)
                                break;

                            writer.Write(buffer, 0, bytesRead);
                            samplesRead += bytesRead / bytesPerSample;
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
