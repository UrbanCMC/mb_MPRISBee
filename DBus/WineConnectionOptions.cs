using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace MusicBeePlugin.DBus;

public class WineConnectionOptions(string address) : DBusConnectionOptions(address)
{
    private readonly string address = address;

    protected override ValueTask<SetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        foreach (var line in File.ReadLines("/proc/self/status"))
        {
            if (!line.StartsWith("Uid:", StringComparison.Ordinal))
            {
                continue;
            }
            var parts = line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && uint.TryParse(parts[1], out var userId))
            {
                return new ValueTask<SetupResult>(new SetupResult(address)
                {
                    // This does not work in wine/windows, will try to DLLImport libc
                    SupportsFdPassing = false,
                    UserId = userId.ToString(),
                    MachineId = "N"
                });
            }
            break;
        }
        throw new Exception("Failed to get Linux UserId from /proc/self/status");
    }
}