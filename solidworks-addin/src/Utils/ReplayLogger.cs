using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using TextToCad.SolidWorksAddin.Models;

namespace TextToCad.SolidWorksAddin.Utils
{
    public static class ReplayLogger
    {
        private static readonly object LockObject = new object();
        private static readonly bool ReplayLoggingEnabled;
        private static readonly string ReplayDirectory;

        private static string _sessionId;
        private static string _replayFilePath;
        private static int _sequence;
        private static bool _sessionActive;
        private static bool _isPaused;
        private static int _sessionIndex;
        private static int _lastSessionIndex;

        static ReplayLogger()
        {
            try
            {
                ReplayLoggingEnabled = bool.Parse(ConfigurationManager.AppSettings["EnableReplayLogging"] ?? "true");
                string configuredPath = ConfigurationManager.AppSettings["ReplayLogDirectory"];

                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    ReplayDirectory = Path.Combine(appDataPath, "TextToCad", "replay");
                }
                else
                {
                    ReplayDirectory = configuredPath;
                }

                if (ReplayLoggingEnabled && !Directory.Exists(ReplayDirectory))
                {
                    Directory.CreateDirectory(ReplayDirectory);
                }
            }
            catch (Exception ex)
            {
                ReplayLoggingEnabled = false;
                System.Diagnostics.Debug.WriteLine($"ReplayLogger initialization failed: {ex.Message}");
            }
        }

        public static bool EnsureSession()
        {
            if (!ReplayLoggingEnabled)
                return false;

            if (_sessionActive && !string.IsNullOrWhiteSpace(_sessionId) && !string.IsNullOrWhiteSpace(_replayFilePath))
                return true;

            BeginSession(out _, out _);
            return true;
        }

        public static string GetCurrentSessionId()
        {
            return _sessionId;
        }

        public static string GetCurrentReplayFilePath()
        {
            return _replayFilePath;
        }

        public static bool IsSessionActive()
        {
            return _sessionActive;
        }

        public static bool IsPaused()
        {
            return _isPaused;
        }

        public static int GetSessionIndex()
        {
            return _sessionIndex;
        }

        public static int GetLastSessionIndex()
        {
            return _lastSessionIndex;
        }

        public static bool BeginSession(out string sessionId, out string replayFilePath)
        {
            sessionId = null;
            replayFilePath = null;

            if (!ReplayLoggingEnabled)
                return false;

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            _sessionIndex += 1;
            _sessionId = Guid.NewGuid().ToString("N");
            _replayFilePath = Path.Combine(ReplayDirectory, $"replay_{timestamp}_{_sessionId}.jsonl");
            _sequence = 0;
            _sessionActive = true;
            _isPaused = false;

            sessionId = _sessionId;
            replayFilePath = _replayFilePath;
            return true;
        }

        public static void EndSession()
        {
            if (_sessionActive)
            {
                _lastSessionIndex = _sessionIndex;
            }

            _sessionActive = false;
            _isPaused = false;
            _sessionId = null;
            _replayFilePath = null;
            _sequence = 0;
        }

        public static void PauseSession()
        {
            if (!_sessionActive)
                return;

            _isPaused = true;
        }

        public static void ResumeSession()
        {
            if (!_sessionActive)
                return;

            _isPaused = false;
        }

        public static string GetLatestReplayFilePath()
        {
            if (!Directory.Exists(ReplayDirectory))
                return null;

            var latest = new DirectoryInfo(ReplayDirectory)
                .GetFiles("replay_*.jsonl")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            return latest?.FullName;
        }

        public static string GetReplayDirectory()
        {
            return ReplayDirectory;
        }

        public static void OpenReplayDirectory()
        {
            if (!ReplayLoggingEnabled)
                return;

            try
            {
                if (!Directory.Exists(ReplayDirectory))
                {
                    Directory.CreateDirectory(ReplayDirectory);
                }

                Process.Start("explorer.exe", ReplayDirectory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open replay directory: {ex.Message}");
            }
        }

        public static void AppendEntry(ReplayEntry entry)
        {
            if (!ReplayLoggingEnabled)
                return;

            if (_isPaused)
                return;

            if (entry == null)
                return;

            if (!EnsureSession())
                return;

            lock (LockObject)
            {
                entry.SessionId = _sessionId;
                entry.Sequence = ++_sequence;
                entry.TimestampUtc = DateTime.UtcNow.ToString("o");

                string json = JsonConvert.SerializeObject(entry, Formatting.None);
                File.AppendAllText(_replayFilePath, json + Environment.NewLine);
            }
        }

        public static List<ReplayEntry> LoadEntries(string path, out string error)
        {
            error = null;
            var entries = new List<ReplayEntry>();

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Replay path is empty.";
                return entries;
            }

            if (!File.Exists(path))
            {
                error = $"Replay file not found: {path}";
                return entries;
            }

            try
            {
                foreach (string line in File.ReadLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var entry = JsonConvert.DeserializeObject<ReplayEntry>(line);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed lines but keep loading.
                    }
                }
            }
            catch (Exception ex)
            {
                error = $"Failed to read replay file: {ex.Message}";
            }

            return entries;
        }

        public static bool TryParseReplayCommand(string input, out string replayPath, out string error)
        {
            replayPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            string trimmed = input.Trim();
            if (!trimmed.StartsWith("replay", StringComparison.OrdinalIgnoreCase))
                return false;

            string remainder = trimmed.Substring("replay".Length).Trim();

            if (string.IsNullOrWhiteSpace(remainder) || remainder.Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                replayPath = GetLatestReplayFilePath();
                if (string.IsNullOrWhiteSpace(replayPath))
                {
                    error = "No replay files found for 'replay last'.";
                }
            }
            else
            {
                replayPath = remainder.Trim('"');
            }

            return true;
        }
    }
}
