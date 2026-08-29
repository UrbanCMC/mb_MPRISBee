using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicBeePlugin
{
    public class Logger
    {
        private readonly FileInfo fileInfo;
        private StreamWriter writer;

        public Logger(string path)
        {
            fileInfo = new FileInfo(path);
            writer = null;

            ClearLog();
        }

        public void Close()
        {
            if (writer == null)
            {
                return;
            }

            try
            {
                writer.Close();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Success(string message)
        {
            Write("success", message);
        }

        public void Fail(string message)
        {
            Write("fail", message);
        }

        public void Debug(string message)
        {
            Write("debug", message);
        }

        public void Info(string message)
        {
            Write("info", message);
        }

        public void Warn(string message)
        {
            Write("warn", message);
        }

        public void Error(string message, Exception ex)
        {
            Write("error", $"{message}{Environment.NewLine}{ex}");
        }

        private void ClearLog()
        {
            var path = fileInfo.FullName;
            if (!File.Exists(path))
            {
                return;
            }

            string content;
            using (var reader = File.OpenText(path))
            {
                content = reader.ReadToEnd();
            }

            var matches = Regex.Matches(content, @"version.*?starting");
            if (matches.Count >= 10)
            {
                File.WriteAllText(path, "");
            }
        }

        private void Write(string type, string message)
        {
            Console.WriteLine($"MPrisBee [{type.ToUpper()}] {message}");

            if (writer == null)
            {
                writer = new StreamWriter(fileInfo.FullName, true, Encoding.UTF8);
                writer.AutoFlush = false;
            }

            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type.ToUpper()}] {message}");
            writer.Flush();
        }
    }
}