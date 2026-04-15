using System;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace MusicBeePlugin.DBus;

public class WineConnectionOptions(string address) : DBusConnectionOptions(address)
{
    private readonly string address = address;

    protected override ValueTask<SetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        var dbusSessionAddress = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
        if (dbusSessionAddress == null)
        {
            throw new Exception("DBUS_SESSION_BUS_ADDRESS environment variable not set");
        }

        if (!int.TryParse(dbusSessionAddress.Replace("unix:path=/run/user/", string.Empty).Replace("/bus", string.Empty), out var userId))
        {
            throw new Exception("Failed to get UserId from DBUS_SESSION_BUS_ADDRESS");
        }

        return new ValueTask<SetupResult>(new SetupResult(address)
        {
            // This does not work in wine/windows, will try to DLLImport libc
            SupportsFdPassing = false,
            UserId = userId.ToString(),
            MachineId = "N"
        });
    }
}