using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MusicBeePlugin.DBus;
using MusicBeePlugin.DBus.Enums;
using Tmds.DBus.Protocol;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private readonly string winePrefix;
        private readonly PluginInfo about = new();

        private MusicBeeApiInterface mbApiInterface;
        private Logger logger;

        private DBusConnection dbusConnection;
        private DBusMediaPlayer dbusPlayer;
        private bool suspended;

        public Plugin()
        {
            winePrefix = Environment.GetEnvironmentVariable("WINEPREFIX");
            if (string.IsNullOrEmpty(winePrefix))
            {
                throw new PlatformNotSupportedException("WINEPREFIX not set. MusicBee appears to not be running in wine.");
            }

            if (!winePrefix.EndsWith("/"))
            {
                winePrefix += "/";
            }

            // from https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs
            AppDomain.CurrentDomain.AssemblyResolve += (object _, ResolveEventArgs args) =>
            {
                var assemblyFile = args.Name.Contains(",")
                    ? args.Name.Substring(0, args.Name.IndexOf(','))
                    : args.Name;

                assemblyFile += ".dll";

                var absoluteFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                var targetPath = Path.Combine(absoluteFolder, "MPRISBee", assemblyFile);

                try
                {
                    Debug.WriteLine($"MPRISBee D: Trying to load assembly {targetPath}");
                    return Assembly.LoadFile(targetPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MPRISBee E: Failed to load assembly {targetPath}: {ex.Message}");
                    return null;
                }
            };
        }

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MPRISBee";
            about.Description = "Sends MusicBee's status outside wine";
            about.Author = "Kyletsit";
            about.TargetApplication = ""; //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1; // your plugin version
            about.VersionMinor = 0;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0; // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            var logDirectory = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), "MPRISBee");
            Directory.CreateDirectory(logDirectory);
            logger = new Logger(Path.Combine(logDirectory, "mb_MPRISBee.log"));

            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            var dataPath = mbApiInterface.Setting_GetPersistentStoragePath();

            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            var dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            if (suspended)
            {
                return;
            }

            try
            {
                dbusConnection.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error("Error during shutdown", ex);
            }

            logger.Close();
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public async Task ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            if (suspended)
            {
                return;
            }

            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                {
                    logger.Info("Plugin startup");

                    try
                    {
                        logger.Info("Connecting to DBUS...");
                        dbusConnection = new DBusConnection(new WineConnectionOptions(DBusAddress.Session!));
                        await dbusConnection.ConnectAsync();

                        logger.Info("Registering as MPRIS media player...");
                        dbusPlayer = new DBusMediaPlayer(dbusConnection, mbApiInterface);
                        await dbusPlayer.AddToDBusAsync();
                    }
                    catch (Exception ex)
                    {
                        suspended = true;
                        logger.Error("Failed to connect to DBUS. Aborting startup", ex);
                        return;
                    }

                    SendPlayState();
                    SendShuffle();
                    SendLoopStatus();

                    Console.WriteLine(mbApiInterface.Player_GetPlayState());

                    if (mbApiInterface.Player_GetPlayState() is PlayState.Paused or PlayState.Playing)
                    {
                        SendMetadataChange();
                        SendAvailablePlayerActions();
                    }

                    logger.Info("Startup completed");
                    break;
                }

                case NotificationType.PlayStateChanged:
                {
                    SendPlayState();
                    break;
                }

                case NotificationType.TrackChanged:
                {
                    SendMetadataChange();
                    SendAvailablePlayerActions();
                    break;
                }

                case NotificationType.PlayerShuffleChanged:
                {
                    SendShuffle();
                    break;
                }

                case NotificationType.PlayerRepeatChanged:
                {
                    SendLoopStatus();
                    break;
                }

                case NotificationType.VolumeLevelChanged:
                {
                    VolumeChange();
                    break;
                }

                case NotificationType.VolumeMuteChanged:
                {
                    MuteChange();
                    break;
                }
            }
        }

        private static string MakeTrackId(string url)
        {
            return "/org/musicbee/track/"
                + BitConverter.ToString(
                        SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(url)))
                    .Replace("-", "")
                    .ToLower();
        }

        private void SendPlayState()
        {
            var playbackStatus = PlaybackStatus.Playing;
            switch (mbApiInterface.Player_GetPlayState())
            {
                case PlayState.Playing:
                    playbackStatus = PlaybackStatus.Playing;
                    break;

                case PlayState.Paused:
                case PlayState.Loading:
                    playbackStatus = PlaybackStatus.Paused;
                    break;

                case PlayState.Stopped:
                case PlayState.Undefined:
                    playbackStatus =  PlaybackStatus.Stopped;
                    break;
            }

            try
            {
                dbusPlayer.PlaybackStatus = playbackStatus.ToString();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to send play state", ex);
            }
        }

        private void SendMetadataChange()
        {
            mbApiInterface.NowPlaying_GetFileTags(
                [
                    MetaDataType.TrackTitle, MetaDataType.Artist, MetaDataType.Album,
                    MetaDataType.DiscNo, MetaDataType.TrackNo,
                    MetaDataType.AlbumArtist, MetaDataType.Composer, MetaDataType.Lyricist,
                    MetaDataType.Genres, MetaDataType.BeatsPerMin, MetaDataType.Year,
                    MetaDataType.Rating, MetaDataType.Comment
                ],
                out var tags);

            var (title, artist, album, discNo, trackNo, albumArtist, composer, lyricist, genres, beatsPerMin, year, rating, comment) =
                (tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7], tags[8], tags[9], tags[10], tags[11], tags[12]);

            var fileUrl = mbApiInterface.NowPlaying_GetFileUrl();
            var trackId = MakeTrackId(fileUrl);
            var length = (long)mbApiInterface.NowPlaying_GetDuration() * 1000;

            var metadata = new Dictionary<string, VariantValue>
            {
                ["mpris:trackid"] = trackId,
                ["xesam:title"] = title,
                ["mpris:length"] = length,
                ["xesam:artist"] = artist,
                ["xesam:album"] = album,
                ["xesam:albumArtist"] = albumArtist,
                ["xesam:url"] = GetUnixFileUrl(fileUrl),

                ["xesam:discNumber"] = discNo,
                ["xesam:trackNumber"] = trackNo,
                ["xesam:composer"] =  composer,
                ["xesam:lyricist"] = lyricist,
                ["xesam:genre"] = genres,
                ["xesam:audioBPM"] = beatsPerMin,
                ["xesam:contentCreated"] = year,
                ["xesam:userRating"] = rating,
                ["xesam:comment"] = comment
            };

            try
            {
                dbusPlayer.Metadata = metadata;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to update metadata", ex);
            }

            Task.Run(() => WaitForArtUpdate(trackId));
        }

        private async Task WaitForArtUpdate(string trackId)
        {
            var artworkUrl = mbApiInterface.NowPlaying_GetArtworkUrl();

            var tries = 1;
            while (string.IsNullOrWhiteSpace(artworkUrl))
            {
                if (tries > 3)
                {
                    return;
                }

                await Task.Delay(50 * tries);
                artworkUrl = mbApiInterface.NowPlaying_GetArtworkUrl();
                Console.WriteLine($"MPRISBee D: artwork url: {artworkUrl}");

                tries += 1;
            }

            SendArtUpdate(trackId, artworkUrl);
        }

        private void SendArtUpdate(string trackId, string artworkUrl)
        {
            if (!dbusPlayer.Metadata.ContainsKey("mpris:trackid") || dbusPlayer.Metadata["mpris:trackid"] != trackId)
            {
                return;
            }

            var newMetadata = new Dictionary<string, VariantValue>(dbusPlayer.Metadata) { { "mpris:artUrl", GetUnixFileUrl(artworkUrl) } };

            try
            {
                dbusPlayer.Metadata = newMetadata;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to add artUrl to metadata", ex);
            }
        }

        private void SendShuffle()
        {
            var shuffle = mbApiInterface.Player_GetShuffle() || mbApiInterface.Player_GetAutoDjEnabled();

            try
            {
                dbusPlayer.Shuffle = shuffle;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to update shuffle state", ex);
            }
        }

        private void SendLoopStatus()
        {
            var loopStatus = LoopStatus.None;
            switch (mbApiInterface.Player_GetRepeat())
            {
                case RepeatMode.All:
                    loopStatus = LoopStatus.Playlist;
                    break;

                case RepeatMode.One:
                    loopStatus = LoopStatus.Track;
                    break;

                case RepeatMode.None:
                    loopStatus = LoopStatus.None;
                    break;
            }

            try
            {
                dbusPlayer.LoopStatus = loopStatus.ToString();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to update loop state", ex);
            }
        }

        private void SendVolumeChange(float vol)
        {
            dbusPlayer.Volume = vol;
        }

        private void VolumeChange()
        {
            var mute = mbApiInterface.Player_GetMute();
            if (mute)
            {
                return;
            }

            SendVolumeChange(mbApiInterface.Player_GetVolume());
        }

        private void MuteChange()
        {
            var mute = mbApiInterface.Player_GetMute();
            if (mute)
            {
                SendVolumeChange(0);
            }
            else
            {
                SendVolumeChange(mbApiInterface.Player_GetVolume());
            }
        }

        private void SendAvailablePlayerActions()
        {
            dbusPlayer.CanGoNext = mbApiInterface.NowPlayingList_IsAnyFollowingTracks();
            dbusPlayer.CanGoPrevious = mbApiInterface.NowPlayingList_IsAnyPriorTracks();
        }

        private string GetUnixFileUrl(string fileUrl)
        {
            var unixUrl = fileUrl.Replace(@"\", "/").Replace("C:/", $"{winePrefix}drive_c/").Replace("Z:/", "/");
            return $"file://{unixUrl}";
        }
    }
}
