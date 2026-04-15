using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace MusicBeePlugin.DBus;

public class DBusMediaPlayer
{
    private const string ObjectPath = "/org/mpris/MediaPlayer2";
    private const string ServiceName = "org.mpris.MediaPlayer2.MusicBee";

    private readonly Plugin.MusicBeeApiInterface mbApiInterface;
    private readonly DBusConnection connection;
    private readonly PathHandler pathHandler;
    private readonly MediaPlayerHandler mediaPlayerHandler;
    private readonly MediaPlayerPlayerHandler playerHandler;

    private bool emitSignals;

    public DBusMediaPlayer(DBusConnection connection, Plugin.MusicBeeApiInterface mbApiInterface)
    {
        this.connection = connection;
        this.mbApiInterface = mbApiInterface;
        pathHandler = new PathHandler(ObjectPath);
        mediaPlayerHandler = new MediaPlayerHandler(this) { PathHandler = pathHandler };
        playerHandler = new MediaPlayerPlayerHandler(this) { PathHandler = pathHandler };
        pathHandler.Add(mediaPlayerHandler);
        pathHandler.Add(playerHandler);

        Identity = "MusicBee";
        DesktopEntry = "MusicBee";
        CanQuit = false;
        CanRaise = false;
        CanSetFullscreen = false;
        HasTrackList = false;
        SupportedUriSchemes = ["file"];
        SupportedMimeTypes = [""];
        PlaybackStatus = "Stopped";
        MinimumRate = 1.0;
        MaximumRate = 1.0;
        CanGoNext = true;
        CanGoPrevious = true;
        CanPlay = true;
        CanPause = true;
        CanSeek = true;
        CanControl = true;
        Metadata = new Dictionary<string, VariantValue>();
    }

    // The SourceGenerator generates properties which are abstract when writable and non-abstract when not writable.
    // For the non-abstract properties, use the handler property as a backing field.
    // For the abstract properties, introduce a backing field in this class.
    // DBus types are non-nullable, so the properties implemented here are non-nullable as well.

    private bool fullscreen;

    private bool Fullscreen
    {
        get => fullscreen;
        set
        {
            fullscreen = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "Fullscreen", value);
        }
    }

    private string loopStatus = "None";

    public string LoopStatus
    {
        get => loopStatus;
        set
        {
            loopStatus = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(playerHandler.InterfaceName, "LoopStatus", value);
        }
    }

    private double rate = 1.0;

    private double Rate
    {
        get => rate;
        set
        {
            rate = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "Rate", value);
        }
    }

    private bool shuffle;

    public bool Shuffle
    {
        get => shuffle;
        set
        {
            shuffle = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "Shuffle", value);
        }
    }

    private double volume = 1.0;

    public double Volume
    {
        get => volume;
        set
        {
            volume = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "Volume", value);
        }
    }

    private string Identity
    {
        get => mediaPlayerHandler.Identity ?? "";
        set
        {
            mediaPlayerHandler.Identity = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "Identity", value);
        }
    }

    private bool CanQuit
    {
        get => mediaPlayerHandler.CanQuit;
        set
        {
            mediaPlayerHandler.CanQuit = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "CanQuit", value);
        }
    }

    private bool CanRaise
    {
        get => mediaPlayerHandler.CanRaise;
        set
        {
            mediaPlayerHandler.CanRaise = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "CanRaise", value);
        }
    }

    private bool HasTrackList
    {
        get => mediaPlayerHandler.HasTrackList;
        set
        {
            mediaPlayerHandler.HasTrackList = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "HasTrackList", value);
        }
    }

    private string[] SupportedUriSchemes
    {
        get => mediaPlayerHandler.SupportedUriSchemes ?? [];
        set
        {
            ThrowIfAnyElementIsNull(value ?? throw new ArgumentNullException(nameof(value)));
            mediaPlayerHandler.SupportedUriSchemes = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "SupportedUriSchemes", VariantValue.Array(value));
        }
    }

    private string[] SupportedMimeTypes
    {
        get => mediaPlayerHandler.SupportedMimeTypes ?? [];
        set
        {
            ThrowIfAnyElementIsNull(value ?? throw new ArgumentNullException(nameof(value)));
            mediaPlayerHandler.SupportedMimeTypes = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "SupportedMimeTypes", VariantValue.Array(value));
        }
    }

    private bool CanSetFullscreen
    {
        get => mediaPlayerHandler.CanSetFullscreen;
        set
        {
            mediaPlayerHandler.CanSetFullscreen = value;
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "CanSetFullscreen", value);
        }
    }

    private string DesktopEntry
    {
        get => mediaPlayerHandler.DesktopEntry ?? "";
        set
        {
            mediaPlayerHandler.DesktopEntry = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(mediaPlayerHandler.InterfaceName, "DesktopEntry", value);
        }
    }

    public string PlaybackStatus
    {
        get => playerHandler.PlaybackStatus ?? "";
        set
        {
            playerHandler.PlaybackStatus = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(playerHandler.InterfaceName, "PlaybackStatus", value);
        }
    }

    private double MinimumRate
    {
        get => playerHandler.MinimumRate;
        set
        {
            playerHandler.MinimumRate = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "MinimumRate", value);
        }
    }

    private double MaximumRate
    {
        get => playerHandler.MaximumRate;
        set
        {
            playerHandler.MaximumRate = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "MaximumRate", value);
        }
    }

    public bool CanGoNext
    {
        get => playerHandler.CanGoNext;
        set
        {
            playerHandler.CanGoNext = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "CanGoNext", value);
        }
    }

    public bool CanGoPrevious
    {
        get => playerHandler.CanGoPrevious;
        set
        {
            playerHandler.CanGoPrevious = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "CanGoPrevious", value);
        }
    }

    private bool CanPlay
    {
        get => playerHandler.CanPlay;
        set
        {
            playerHandler.CanPlay = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "CanPlay", value);
        }
    }

    private bool CanPause
    {
        get => playerHandler.CanPause;
        set
        {
            playerHandler.CanPause = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "CanPause", value);
        }
    }

    private bool CanSeek
    {
        get => playerHandler.CanSeek;
        set
        {
            playerHandler.CanSeek = value;
            EmitPropertyChanged(playerHandler.InterfaceName, "CanSeek", value);
        }
    }

    private bool CanControl
    {
        get => playerHandler.CanControl;
        set
        {
            playerHandler.CanControl = value;
            // note: no PropertiesChanged signal is emitted for CanControl.
        }
    }

    private long Position => mbApiInterface.Player_GetPosition() * 1000;

    public Dictionary<string, VariantValue> Metadata
    {
        get => playerHandler.Metadata ?? throw new InvalidOperationException($"{nameof(Metadata)} should be initialized");
        set
        {
            playerHandler.Metadata = value ?? throw new ArgumentNullException(nameof(Metadata));
            var dict = new Dict<string, VariantValue>(value);
            EmitPropertyChanged(playerHandler.InterfaceName, "Metadata", dict);
        }
    }

    public async Task AddToDBusAsync()
    {
        connection.AddMethodHandler(pathHandler);
        emitSignals = true;
        await connection.RequestNameAsync(ServiceName);
    }

    private void Next()
    {
        mbApiInterface.Player_PlayNextTrack();
    }

    private void Previous()
    {
        mbApiInterface.Player_PlayPreviousTrack();
    }

    private void Pause()
    {
        if (mbApiInterface.Player_GetPlayState() is Plugin.PlayState.Playing)
        {
            mbApiInterface.Player_PlayPause();
        }
    }

    private void PlayPause()
    {
        mbApiInterface.Player_PlayPause();
    }

    private void Stop()
    {
        mbApiInterface.Player_Stop();
    }

    private void Play()
    {
        if (mbApiInterface.Player_GetPlayState() is Plugin.PlayState.Paused or Plugin.PlayState.Stopped)
        {
            mbApiInterface.Player_PlayPause();
        }
    }

    public void OnSeeked(int position)
    {
        playerHandler.OnSeeked(position);
    }

    private void Seek(long offset)
    {
        var currentPos = mbApiInterface.Player_GetPosition();
        var newPos = Math.Max(0, currentPos + (int)(offset / 1000));
        mbApiInterface.Player_SetPosition(newPos);
    }

    private void SetPosition(ObjectPath trackId, long position)
    {
        mbApiInterface.Player_SetPosition((int)(position / 1000));
    }

    private void OpenUri(string uri)
    {
        Console.WriteLine($"OpenUri requested: {uri}");
    }

    private void SetLoopStatus(string loopStatus)
    {
        Enum.TryParse<Enums.LoopStatus>(loopStatus, out var loopEnum);
        var repeatMode = Plugin.RepeatMode.None;
        switch (loopEnum)
        {
            case Enums.LoopStatus.None:
                repeatMode = Plugin.RepeatMode.None;
                break;

            case Enums.LoopStatus.Playlist:
                repeatMode = Plugin.RepeatMode.All;
                break;

            case Enums.LoopStatus.Track:
                repeatMode = Plugin.RepeatMode.One;
                break;
        }

        mbApiInterface.Player_SetRepeat(repeatMode);
    }

    private void SetShuffle(bool shuffle)
    {
        mbApiInterface.Player_SetShuffle(shuffle);
    }

    private void SetVolume(double volume)
    {
        mbApiInterface.Player_SetVolume((float)volume);
    }

    private void EmitPropertyChanged(string interfaceName, string name, VariantValue value)
    {
        if (!emitSignals)
        {
            return;
        }

        var writer = connection.GetMessageWriter();
        writer.WriteSignalHeader(null, ObjectPath, "org.freedesktop.DBus.Properties", "PropertiesChanged", "sa{sv}as");
        writer.WriteString(interfaceName);
        writer.WriteDictionary([new KeyValuePair<string, VariantValue>(name, value)]);
        writer.WriteArray(Array.Empty<string>());
        connection.TrySendMessage(writer.CreateMessage());
        writer.Dispose();
    }

    private static void ThrowIfAnyElementIsNull(string[] value)
    {
        if (Array.IndexOf(value, null) != -1)
        {
            throw new ArgumentException("Array contains null elements.", nameof(value));
        }
    }

    private sealed class MediaPlayerHandler(DBusMediaPlayer player) : OrgMprisMediaPlayer2Handler
    {
        public override Connection Connection => player.connection.AsConnection();

        public override bool Fullscreen
        {
            get => player.Fullscreen;
            set => player.Fullscreen = value;
        }

        protected override ValueTask OnRaiseAsync(Message request)
        {
            return default;
        }

        protected override ValueTask OnQuitAsync(Message request)
        {
            return default;
        }
    }

    private sealed class MediaPlayerPlayerHandler(DBusMediaPlayer player) : OrgMprisMediaPlayer2PlayerHandler
    {
        public override Connection Connection => player.connection.AsConnection();

        public override string LoopStatus
        {
            get => player.LoopStatus;
            set => player.SetLoopStatus(value);
        }

        public override long Position
        {
            get => player.Position;
            set { /* Not actually supported by the spec */ }
        }

        public override double Rate
        {
            get => player.Rate;
            set => player.Rate = value;
        }

        public override bool Shuffle
        {
            get => player.Shuffle;
            set => player.SetShuffle(value);
        }

        public override double Volume
        {
            get => player.Volume;
            set => player.SetVolume(value);
        }

        public void OnSeeked(int position)
        {
            EmitSeeked(position * 1000);
        }

        protected override ValueTask OnNextAsync(Message request)
        {
            player.Next();
            return default;
        }

        protected override ValueTask OnPreviousAsync(Message request)
        {
            player.Previous();
            return default;
        }

        protected override ValueTask OnPauseAsync(Message request)
        {
            player.Pause();
            return default;
        }

        protected override ValueTask OnPlayPauseAsync(Message request)
        {
            player.PlayPause();
            return default;
        }

        protected override ValueTask OnStopAsync(Message request)
        {
            player.Stop();
            return default;
        }

        protected override ValueTask OnPlayAsync(Message request)
        {
            player.Play();
            return default;
        }

        protected override ValueTask OnSeekAsync(Message request, long offset)
        {
            player.Seek(offset);
            return default;
        }

        protected override ValueTask OnSetPositionAsync(Message request, ObjectPath trackId, long position)
        {
            player.SetPosition(trackId, position);
            return default;
        }

        protected override ValueTask OnOpenUriAsync(Message request, string uri)
        {
            player.OpenUri(uri);
            return default;
        }
    }
}