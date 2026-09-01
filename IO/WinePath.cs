using System;
using System.Diagnostics;
using System.Text;

namespace MusicBeePlugin.IO;

public static class WinePath
{
    private static Logger logger;

    public static void Init(Logger logger)
    {
        WinePath.logger = logger;
    }

    public static string GetUnixFileUrl(string windowsPath)
    {
        try
        {
            var unixPath = CallWinePath(windowsPath, true);

            // Convert path from Windows-1252 to UTF8 (should only produce invalid output if the input is already malformed)
            var cp1252 = Encoding.GetEncoding(1252);
            unixPath = Encoding.UTF8.GetString(cp1252.GetBytes(unixPath));

            return $"file://{unixPath}";
        }
        catch (Exception ex)
        {
            logger.Error("Failed to convert windows path to unix.", ex);
            return "";
        }
    }

    public static string GetWindowsPath(string unixPath)
    {
        if (unixPath.StartsWith("file://"))
        {
            unixPath = unixPath.Substring(7);
        }

        try
        {
            return CallWinePath(unixPath, false);
        }
        catch (Exception ex)
        {
            logger.Error("Failed to convert unix path to windows.", ex);
            return "";
        }
    }

    private static string CallWinePath(string input, bool toUnixPath)
    {
        // Ensure paths are properly quoted
        if (!input.StartsWith("\"") || input.StartsWith("'"))
        {
            input = $"\"{input}\"";
        }

        string[] args = [toUnixPath ? "-u" : "-w", input];
        var startInfo = new ProcessStartInfo("winepath", string.Join(" ", args))
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(startInfo);
            process!.WaitForExit();

            return process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
        }
        catch (Exception ex)
        {
            logger.Error("Failed to run winepath.", ex);
            return input;
        }
    }
}