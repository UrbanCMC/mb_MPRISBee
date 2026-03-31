using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MPrisBee;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    // Incoming messages from MPRIS from mprisbee-bridge
    [JsonConverter(typeof(MprisMessageConverter))]
    public abstract class MprisMessage
    {
        public string Event { get; set; }
    }

    public class MPRISNext : MprisMessage { }
    public class MPRISPrevious : MprisMessage { }
    public class MPRISPause : MprisMessage { }
    public class MPRISPlayPause : MprisMessage { }
    public class MPRISStop : MprisMessage { }
    public class MPRISPlay : MprisMessage { }

    public class MPRISSeek : MprisMessage
    {
        public long Offset { get; set; }
    }

    public class MPRISPosition : MprisMessage
    {
        public string TrackId { get; set; }
        public long Position { get; set; }
    }

    public class MPRISGetPosition : MprisMessage { }

    public enum LoopStatus
    {
        None,
        Track,
        Playlist
    }

    public class MPRISLoopStatus : MprisMessage
    {
        public static LoopStatus RepeatToLoop(RepeatMode mode)
        {
            switch (mode)
            {
                case RepeatMode.All:
                    return LoopStatus.Playlist;
                case RepeatMode.One:
                    return LoopStatus.Track;
                case RepeatMode.None:
                default:
                    return LoopStatus.None;
            }
        }

        public static RepeatMode LoopToRepeat(LoopStatus status)
        {
            switch (status)
            {
                case LoopStatus.Playlist:
                    return RepeatMode.All;
                case LoopStatus.Track:
                    return RepeatMode.One;
                case LoopStatus.None:
                default:
                    return RepeatMode.None;
            }
        }

        public LoopStatus Status { get; set; }
    }

    public class MPRISShuffle : MprisMessage
    {
        public bool Shuffle { get; set; }
    }

    public class MPRISVolume : MprisMessage
    {
        public double Volume { get; set; }
    }

    public class MprisMessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(MprisMessage).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            string eventType = (string)obj["event"];

            MprisMessage message = eventType switch
            {
                "next" => new MPRISNext(),
                "previous" => new MPRISPrevious(),
                "pause" => new MPRISPause(),
                "playpause" => new MPRISPlayPause(),
                "stop" => new MPRISStop(),
                "play" => new MPRISPlay(),
                "seek" => new MPRISSeek()
                {
                    Offset = (long)obj["offset"],
                },
                "position" => new MPRISPosition()
                {
                    TrackId = (string)obj["trackid"],
                    Position = (long)obj["position"],
                },
                "getposition" => new MPRISGetPosition(),
                "loop_status" => new MPRISLoopStatus()
                {
                    Status = obj["status"].ToObject<LoopStatus>(),
                },
                "shuffle" => new MPRISShuffle()
                {
                    Shuffle = (bool)obj["shuffle"],
                },
                "volume" => new MPRISVolume()
                {
                    Volume = (double)obj["volume"],
                },
                _ => throw new JsonSerializationException($"Unknown event type: {eventType}")
            };

            serializer.Populate(obj.CreateReader(), message);
            return message;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject obj = JObject.FromObject(value);
            obj.WriteTo(writer);
        }
    }

    public class MprisMetadata
    {
        [JsonProperty("trackid")]
        public string TrackId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("artist")]
        public string[] Artists { get; set; }

        [JsonProperty("album")]
        public string Album { get; set; }

        [JsonProperty("file_url")]
        public string FileUrl { get; set; }

        // Optional fields vvvv

        [JsonProperty("disc_number", NullValueHandling = NullValueHandling.Ignore)]
        public string DiscNumber { get; set; }

        [JsonProperty("track_number", NullValueHandling = NullValueHandling.Ignore)]
        public string TrackNumber { get; set; }

        [JsonProperty("album_artist", NullValueHandling = NullValueHandling.Ignore)]
        public string[] AlbumArtist { get; set; }

        [JsonProperty("composer", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Composers { get; set; }

        [JsonProperty("lyricist", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Lyricist { get; set; }

        [JsonProperty("genre", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Genres { get; set; }

        [JsonProperty("audio_bpm", NullValueHandling = NullValueHandling.Ignore)]
        public string AudioBpm { get; set; }

        [JsonProperty("content_created", NullValueHandling = NullValueHandling.Ignore)]
        public string ContentCreated { get; set; }

        [JsonProperty("user_rating", NullValueHandling = NullValueHandling.Ignore)]
        public string UserRating { get; set; }

        [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Comments { get; set; }
    }

    // Outgoing messages to mprisbee-bridge
    public class MetadataChangeEvent
    {
        [JsonProperty("event")]
        public string Event => "metachange";

        [JsonProperty("metadata")]
        public MprisMetadata Metadata { get; set; }
    }

    public class ArtUpdateEvent
    {
        [JsonProperty("event")]
        public string Event => "artupdate";

        [JsonProperty("trackid")]
        public string TrackId { get; set; }

        [JsonProperty("albumartpath")]
        public string Path { get; set; }
    }

    public class SeekEvent
    {
        [JsonProperty("event")]
        public string Event => "seek";

        [JsonProperty("offset")]
        public long Offset { get; set; }
    }

    public class PauseEvent
    {
        [JsonProperty("event")]
        public string Event => "pause";
    }

    public class PlayEvent
    {
        [JsonProperty("event")]
        public string Event => "play";
    }

    public class StopEvent
    {
        [JsonProperty("event")]
        public string Event => "stop";
    }

    public class PositionEvent
    {
        [JsonProperty("event")]
        public string Event => "position";

        [JsonProperty("position")]
        public long position { get; set; }
    }

    public class VolumeChangeEvent
    {
        [JsonProperty("event")]
        public string Event => "volume";

        [JsonProperty("volume")]
        public double volume { get; set; }
    }

    public class ShuffleEvent
    {
        [JsonProperty("event")]
        public string Event => "shuffle";

        [JsonProperty("shuffle")]
        public bool shuffle { get; set; }
    }

    public class LoopStatusEvent
    {
        [JsonProperty("event")]
        public string Event => "loopstatus";

        [JsonProperty("status")]
        public string status { get; set; }
    }

    public class ExitEvent
    {
        [JsonProperty("event")]
        public string Event => "exit";
    }

    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        private CancellationTokenSource listener_cts = new CancellationTokenSource();
        private Logger logger;

        private bool suspended;
        private UnixSocket wineOut;

        public Plugin()
        {
            // from https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs
            AppDomain.CurrentDomain.AssemblyResolve += (object _, ResolveEventArgs args) =>
            {
                string assemblyFile = args.Name.Contains(",")
                    ? args.Name.Substring(0, args.Name.IndexOf(','))
                    : args.Name;

                assemblyFile += ".dll";

                string absoluteFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                string targetPath = Path.Combine(absoluteFolder, "MPRISBee", assemblyFile);

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
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 0;  // your plugin version
            about.VersionMinor = 1;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            var logDirectory = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), "MPrisBee");
            Directory.CreateDirectory(logDirectory);
            logger = new Logger(Path.Combine(logDirectory, "mb_MPrisBee.log"));

            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            if (suspended)
            {
                return;
            }

            var exitEvent = new ExitEvent();
            string json = JsonConvert.SerializeObject(exitEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Error during shutdown", ex);
            }

            logger.Close();
            StopListening();
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

                        var uid = 1000;

                        try
                        {
                            var path = $"/tmp/mprisbee{uid}/wine.sock";
                            logger.Info($"Socket path: {path}");
                            wineOut = new UnixSocket(logger, path);

                            StartListening();
                        }
                        catch (Exception ex)
                        {
                            suspended = true;
                            logger.Error("Failed to create socket. Aborting startup", ex);
                            return;
                        }

                        SendPlayState();
                        SendShuffle();
                        SendLoopStatus();

                        Console.WriteLine(mbApiInterface.Player_GetPlayState());

                        if (mbApiInterface.Player_GetPlayState() == PlayState.Paused || mbApiInterface.Player_GetPlayState() == PlayState.Playing)
                        {
                            SendMetadataChange();
                        }

                        logger.Info("Startup completed");
                    }
                    break;

                case NotificationType.PlayStateChanged:
                    {
                        Console.WriteLine($"MPRISBee D: PlayState changed");
                        SendPlayState();
                    }
                break;

                case NotificationType.TrackChanged:
                    {
                        Console.WriteLine($"MPRISBee D: Track changed");
                        SendMetadataChange();
                    }
                    break;

                case NotificationType.PlayerShuffleChanged:
                    {
                        Console.WriteLine($"MPRISBee D: Shuffle changed");
                        SendShuffle();
                    }
                    break;

                case NotificationType.PlayerRepeatChanged:
                    {
                        Console.WriteLine($"MPRISBee D: Repeat changed");
                        SendLoopStatus();
                    }
                    break;

                case NotificationType.VolumeLevelChanged:
                    {
                        Console.WriteLine($"MPRISBee D: Volume changed");
                        VolumeChange();
                    }
                    break;

                case NotificationType.VolumeMuteChanged:
                    {
                        Console.WriteLine($"MPRISBee D: Mute changed");
                        MuteChange();
                    }
                    break;
            }
        }

        private static string MakeTrackId(string url)
        {
            return "/org/musicbee/track/" + BitConverter.ToString(
                    SHA256.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(url)
                        )
                ).Replace("-", "").ToLower();
        }

        private void SendPlayState()
        {
            var json_event = "";

            switch (mbApiInterface.Player_GetPlayState())
            {
                case PlayState.Playing:
                    {
                        var playEvent = new PlayEvent();
                        json_event = JsonConvert.SerializeObject(playEvent);
                    }
                    break;

                case PlayState.Paused:
                case PlayState.Loading:
                    {
                        var pauseEvent = new PauseEvent();
                        json_event = JsonConvert.SerializeObject(pauseEvent);
                    }
                    break;

                case PlayState.Stopped:
                case PlayState.Undefined:
                    {
                        var stopEvent = new StopEvent();
                        json_event = JsonConvert.SerializeObject(stopEvent);
                    }
                    break;
            }

            try
            {
                wineOut.WriteStringNLTerminated(json_event);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to send play state", ex);
            }
        }

        private void SendMetadataChange()
        {
            string[] tags;

            mbApiInterface.NowPlaying_GetFileTags(new[] {   MetaDataType.TrackTitle, MetaDataType.Artist, MetaDataType.Album,
                                                MetaDataType.DiscNo, MetaDataType.TrackNo,
                                                MetaDataType.AlbumArtist, MetaDataType.Composer, MetaDataType.Lyricist,
                                                MetaDataType.Genres, MetaDataType.BeatsPerMin, MetaDataType.Year,
                                                MetaDataType.Rating, MetaDataType.Comment }, out tags);
            string Title, Album, DiscNo, TrackNo, BeatsPerMin, Year, Rating;
            string[] Artist = new string[1];
            string[] AlbumArtist = new string[1];
            string[] Composer = new string[1];
            string[] Lyricist = new string[1];
            string[] Genres = new string[1];
            string[] Comment = new string[1];

            (Title,   Artist[0],   Album,   DiscNo,  TrackNo, AlbumArtist[0], Composer[0], Lyricist[0], Genres[0], BeatsPerMin, Year,     Rating,   Comment[0]) =
            (tags[0], tags[1],     tags[2], tags[3], tags[4], tags[5],        tags[6],     tags[7],     tags[8],   tags[9],     tags[10], tags[11], tags[12]  );

            Console.WriteLine($"MPRISBee D: Metadata:");
            Console.WriteLine($"MPRISBee D: {tags}");

            string FileUrl = mbApiInterface.NowPlaying_GetFileUrl();
            Console.WriteLine($"MPRISBee D: file url: {FileUrl}");
            string TrackId = MakeTrackId(FileUrl);

            long Length = mbApiInterface.NowPlaying_GetDuration();

            var metadata = new MprisMetadata
            {
                TrackId = TrackId,
                Title = Title,
                Length = Length,
                Artists = Artist,
                Album = Album,
                FileUrl = FileUrl,

                DiscNumber = DiscNo,
                TrackNumber = TrackNo,
                AlbumArtist = AlbumArtist,
                Composers = Composer,
                Lyricist = Lyricist,
                Genres = Genres,
                AudioBpm = BeatsPerMin,
                ContentCreated = Year,
                UserRating = Rating,
                Comments = Comment,
            };

            Console.WriteLine(metadata);

            var metadataChangeEvent = new MetadataChangeEvent
            {
                Metadata = metadata,
            };

            string json = JsonConvert.SerializeObject(metadataChangeEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write metadata to socket", ex);
            }

            Task.Run(() => WaitForArtUpdate(TrackId));
        }

        private async Task WaitForArtUpdate(string TrackId)
        {
            var ArtworkUrl = mbApiInterface.NowPlaying_GetArtworkUrl();
            Console.WriteLine($"MPRISBee D: artwork url: {ArtworkUrl}");

            int tries = 1;
            while (string.IsNullOrWhiteSpace(ArtworkUrl))
            {
                if (tries > 3)
                {
                    Console.WriteLine($"MPRISBee D: artwork not found");
                    return;
                }

                await Task.Delay(50 * tries);
                ArtworkUrl = mbApiInterface.NowPlaying_GetArtworkUrl();
                Console.WriteLine($"MPRISBee D: artwork url: {ArtworkUrl}");

                tries += 1;
            }

            SendArtUpdate(TrackId, ArtworkUrl);
        }

        private void SendArtUpdate(string TrackId, string ArtworkUrl)
        {
            var artUpdateEvent = new ArtUpdateEvent
            {
                TrackId = TrackId,
                Path = ArtworkUrl,
            };

            string json = JsonConvert.SerializeObject(artUpdateEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write artwork update to socket", ex);
            }
        }

        private void SendPosition()
        {
            var positionEvent = new PositionEvent()
            {
                position = (long)mbApiInterface.Player_GetPosition()
            };

            string json = JsonConvert.SerializeObject(positionEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write position update to socket", ex);
            }
        }

        private void SendShuffle()
        {
            var shuffleEvent = new ShuffleEvent()
            {
                shuffle = (mbApiInterface.Player_GetShuffle() || mbApiInterface.Player_GetAutoDjEnabled())
            };

            string json = JsonConvert.SerializeObject(shuffleEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write shuffle state to socket", ex);
            }
        }

        private void SendLoopStatus()
        {
            var loopStatusEvent = new LoopStatusEvent()
            {
                status = MPRISLoopStatus.RepeatToLoop(mbApiInterface.Player_GetRepeat()).ToString()
            };

            string json = JsonConvert.SerializeObject(loopStatusEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write loop state to socket", ex);
            }
        }

        private void SendVolumeChange(float vol)
        {
            var volumeChangeEvent = new VolumeChangeEvent()
            {
                volume = (double)vol
            };

            string json = JsonConvert.SerializeObject(volumeChangeEvent);

            try
            {
                wineOut.WriteStringNLTerminated(json);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to write volume change to socket", ex);
            }
        }

        private void VolumeChange()
        {
            var mute = mbApiInterface.Player_GetMute();
            if (mute)
            {
                return;
            }
            else
            {
                SendVolumeChange(mbApiInterface.Player_GetVolume());
            }
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

        private void StartListening()
        {
            Task.Run(() => ListenLoop(listener_cts.Token));
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string json = await ReadMessageAsync();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        try
                        {
                            MprisMessage message = JsonConvert.DeserializeObject<MprisMessage>(json);
                            HandlePlayerEvent(message);
                        }
                        catch (JsonException ex)
                        {
                            logger.Error("Failed to parse JSON", ex);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                logger.Error("Socket I/O failed", ex);
            }
        }

        private void StopListening()
        {
            listener_cts.Cancel();
        }

        private Task<string> ReadMessageAsync()
        {
            return Task.Run(() => wineOut.ReadStringNLTerminated());
        }

        private void HandlePlayerEvent(MprisMessage message)
        {
            switch (message)
            {
                case MPRISNext:
                    mbApiInterface.Player_PlayNextTrack();
                    break;

                case MPRISPrevious:
                    mbApiInterface.Player_PlayPreviousTrack();
                    break;

                case MPRISPause:
                    if (mbApiInterface.Player_GetPlayState() == Plugin.PlayState.Playing)
                    {
                        mbApiInterface.Player_PlayPause();
                    }
                    break;

                case MPRISPlayPause:
                    mbApiInterface.Player_PlayPause();
                    break;

                case MPRISStop:
                    mbApiInterface.Player_Stop();
                    break;

                case MPRISPlay:
                    if (mbApiInterface.Player_GetPlayState() == Plugin.PlayState.Paused || mbApiInterface.Player_GetPlayState() == Plugin.PlayState.Stopped)
                    {
                        mbApiInterface.Player_PlayPause();
                    }
                    break;

                case MPRISSeek Seek:
                    if (int.MinValue >= Seek.Offset && Seek.Offset <= int.MaxValue)
                    {
                        var currentPos = mbApiInterface.Player_GetPosition();
                        mbApiInterface.Player_SetPosition(currentPos + (int)Seek.Offset);
                    }
                    else
                    {
                        throw new OverflowException("Offset value is too large for int.");
                    }
                    break;

                case MPRISPosition Position:
                    if (int.MinValue <= Position.Position && Position.Position <= int.MaxValue)
                    {
                        mbApiInterface.Player_SetPosition((int)Position.Position);
                    }
                    else
                    {
                        throw new OverflowException("Position value is too large for int.");
                    }
                    break;

                case MPRISGetPosition:
                    {
                        SendPosition();
                    }
                    break;

                case MPRISLoopStatus Status:
                    mbApiInterface.Player_SetRepeat(MPRISLoopStatus.LoopToRepeat(Status.Status));
                    break;

                case MPRISShuffle Shuffle:
                    mbApiInterface.Player_SetShuffle(Shuffle.Shuffle);
                    break;

                case MPRISVolume Volume:
                    mbApiInterface.Player_SetVolume((float)Volume.Volume);
                    break;
            }
        }


        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        //public string[] GetProviders()
        //{
        //    return null;
        //}

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        //public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        //{
        //    return null;
        //}

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        //public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        //{
        //    //Return Convert.ToBase64String(artworkBinaryData)
        //    return null;
        //}

        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        //public int OnDockablePanelCreated(Control panel)
        //{
        //  //    return the height of the panel and perform any initialisation here
        //  //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
        //  //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
        //  //    = 0 indicates to MusicBee this control resizeable
        //  //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
        //    float dpiScaling = 0;
        //    using (Graphics g = panel.CreateGraphics())
        //    {
        //        dpiScaling = g.DpiY / 96f;
        //    }
        //    panel.Paint += panel_Paint;
        //    return Convert.ToInt32(100 * dpiScaling);
        //}

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}

        //private void panel_Paint(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.Clear(Color.Red);
        //    TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        //}

    }
}
