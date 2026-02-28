using Avalonia.Threading;
using BrickForceDevTools.Views;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.IO;

namespace BrickForceDevTools
{
    static class Global
    {
        public static bool SkipMissingGeometry = true;
        public static bool DefaultExportAll = true;
        public static bool DefaultExportRegMap = true;
        public static bool DefaultExportGeometry = true;
        public static bool DefaultExportJson = true;
        public static bool DefaultExportObj = true;
        public static bool DefaultExportPlaintext = true;
        public static bool IncludeAssemblyLineInPatchInfo = false;
        public static string DefaultExportLocation = Path.GetFullPath("Export");

        private static int _regMapCount;
        public static int RegMapCount => Volatile.Read(ref _regMapCount);

        // Fires on any change (we’ll update UI from MainWindow)
        public static event Action<int>? RegMapCountChanged;


        public static string settingsFilePath = "settings.json";

        public static MainWindow MainWindowInstance;

        // ---- Logging internals (throttled + batched) ----
        private static readonly ConcurrentQueue<LogOp> _logQueue = new();
        private static int _flushScheduled; // 0/1 flag

        // Tweak if needed:
        private const int FlushDelayMs = 50;     // UI update rate (~20 fps max)
        private const int MaxLogChars = 250_000; // cap total log text size

        private enum LogOpType { Append, AppendLine, ReplaceLastLine }

        private readonly struct LogOp
        {
            public readonly LogOpType Type;
            public readonly string Text;

            public LogOp(LogOpType type, string text)
            {
                Type = type;
                Text = text ?? string.Empty;
            }
        }

        public static void ResetRegMapCount()
        {
            Interlocked.Exchange(ref _regMapCount, 0);
            RegMapCountChanged?.Invoke(0);
        }

        public static int IncrementRegMapCount()
        {
            var v = Interlocked.Increment(ref _regMapCount);
            RegMapCountChanged?.Invoke(v);
            return v;
        }

        public static void PrintLine(string text)
        {
            Enqueue(new LogOp(LogOpType.AppendLine, text));
        }

        public static void Print(string text)
        {
            Enqueue(new LogOp(LogOpType.Append, text));
        }

        public static void PrintReplace(string text)
        {
            Enqueue(new LogOp(LogOpType.ReplaceLastLine, text));
        }

        private static void Enqueue(LogOp op)
        {
            if (MainWindowInstance?.LogTextBlock == null)
                return;

            _logQueue.Enqueue(op);

            // ensure only one flush is scheduled at a time
            if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    // throttle so many logs get batched into one UI update
                    await System.Threading.Tasks.Task.Delay(FlushDelayMs);

                    FlushToUi();

                    Interlocked.Exchange(ref _flushScheduled, 0);

                    // if logs came in during flush, schedule again
                    if (!_logQueue.IsEmpty)
                        Enqueue(new LogOp(LogOpType.Append, string.Empty)); // triggers scheduling
                });
            }
        }

        private static void FlushToUi()
        {
            var tb = MainWindowInstance?.LogTextBlock;
            var sv = MainWindowInstance?.LogScrollViewer;
            if (tb == null) return;

            // Build up appended text in a single buffer (fast)
            var appendSb = new StringBuilder();

            // We apply replace operations to the current text (rare), and append operations batched.
            bool didReplace = false;
            string? replaceText = null;

            while (_logQueue.TryDequeue(out var op))
            {
                // ignore the scheduling no-op
                if (op.Type == LogOpType.Append && op.Text.Length == 0)
                    continue;

                switch (op.Type)
                {
                    case LogOpType.Append:
                        appendSb.Append(op.Text);
                        break;

                    case LogOpType.AppendLine:
                        appendSb.Append(op.Text);
                        appendSb.Append(Environment.NewLine);
                        break;

                    case LogOpType.ReplaceLastLine:
                        didReplace = true;
                        replaceText = op.Text; // keep latest replace request
                        break;
                }
            }

            // Apply replace-last-line if requested
            if (didReplace && replaceText != null)
            {
                tb.Text = ReplaceLastLine(tb.Text ?? string.Empty, replaceText);
            }

            // Apply append batch
            if (appendSb.Length > 0)
            {
                tb.Text += appendSb.ToString();
            }

            // Cap log size to keep UI fast over time
            if (tb.Text.Length > MaxLogChars)
            {
                tb.Text = tb.Text[^MaxLogChars..];
            }

            sv?.ScrollToEnd();
        }

        private static string ReplaceLastLine(string log, string newLineText)
        {
            if (string.IsNullOrEmpty(log))
                return newLineText + Environment.NewLine;

            // Normalize to '\n' for easier indexing
            var normalized = log.Replace("\r\n", "\n");

            // Remove trailing newline(s) so "last line" is a real line
            while (normalized.Length > 0 && normalized[^1] == '\n')
                normalized = normalized[..^1];

            var lastNl = normalized.LastIndexOf('\n');
            if (lastNl >= 0)
            {
                normalized = normalized[..(lastNl + 1)] + newLineText;
            }
            else
            {
                normalized = newLineText;
            }

            // Convert back to Environment.NewLine and ensure it ends with newline
            var restored = normalized.Replace("\n", Environment.NewLine) + Environment.NewLine;
            return restored;
        }
    }
}
