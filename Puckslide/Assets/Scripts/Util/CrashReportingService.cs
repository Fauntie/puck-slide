using System;
using System.Collections.Generic;
using UnityEngine;

public static class CrashReportingService
{
    private static bool s_Initialized;
    private static bool s_Enabled;
    private static NetworkDiagnostics s_Diagnostics;
    private static readonly List<string> s_RedactionTerms = new List<string>();

    public static void Initialize(NetworkDiagnostics diagnostics, bool enabled, IEnumerable<string> redactionTerms = null)
    {
        if (s_Initialized)
        {
            return;
        }

        s_Diagnostics = diagnostics;
        if (redactionTerms != null)
        {
            s_RedactionTerms.AddRange(redactionTerms);
        }

        s_Initialized = true;
        SetEnabled(enabled);
    }

    public static void AddRedactionTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        if (!s_RedactionTerms.Contains(term))
        {
            s_RedactionTerms.Add(term);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (!s_Initialized || s_Enabled == enabled)
        {
            return;
        }

        s_Enabled = enabled;

        if (enabled)
        {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.logMessageReceived += HandleLogMessage;
        }
        else
        {
            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            Application.logMessageReceived -= HandleLogMessage;
        }
    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (!s_Enabled)
        {
            return;
        }

        Exception exception = e.ExceptionObject as Exception;
        string message = exception?.Message ?? "Unhandled exception";
        string stack = exception?.StackTrace ?? string.Empty;
        RecordCrash("unhandled_exception", message, stack);
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!s_Enabled || type != LogType.Exception)
        {
            return;
        }

        RecordCrash("logged_exception", condition, stackTrace);
    }

    private static void RecordCrash(string kind, string message, string stackTrace)
    {
        string sanitizedMessage = Sanitize(message);
        string sanitizedStack = Sanitize(stackTrace);
        s_Diagnostics?.LogEvent(
            "crash",
            sanitizedMessage,
            new Dictionary<string, string>
            {
                {"kind", kind},
                {"stackTrace", Truncate(sanitizedStack, 1024)}
            });
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string sanitized = value.Replace('\n', ' ').Replace('\r', ' ');
        foreach (string term in s_RedactionTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            sanitized = sanitized.Replace(term, "<redacted>");
        }

        return sanitized;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }
}
