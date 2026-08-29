using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace MusicBeePlugin.DBus;

public class DBusMediaPlayer
{
    private const string ObjectPath = "/org/mpris/MediaPlayer2";
    private const string ServiceName = "org.mpris.MediaPlayer2.MusicBee";

    private readonly Plugin.MusicBeeApiInterface mbApiInterface;
    private readonly DBusConnection connection;
    private readonly DBusMediaPlayerHandler handler;

    private bool emitSignals;

    public DBusMediaPlayer(DBusConnection connection, Plugin.MusicBeeApiInterface mbApiInterface)
    {
        this.connection = connection;
        this.mbApiInterface = mbApiInterface;
        handler = new DBusMediaPlayerHandler(connection, this);
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
            EmitPropertyChanged(MediaPlayer2Property.Fullscreen);
        }
    }

    private string loopStatus = "None";

    public string LoopStatus
    {
        get => loopStatus;
        set
        {
            loopStatus = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(PlayerProperty.LoopStatus);
        }
    }

    private double rate = 1.0;

    private double Rate
    {
        get => rate;
        set
        {
            rate = value;
            EmitPropertyChanged(PlayerProperty.Rate);
        }
    }

    private bool shuffle;

    public bool Shuffle
    {
        get => shuffle;
        set
        {
            shuffle = value;
            EmitPropertyChanged(PlayerProperty.Shuffle);
        }
    }

    private double volume = 1.0;

    public double Volume
    {
        get => volume;
        set
        {
            volume = value;
            EmitPropertyChanged(PlayerProperty.Volume);
        }
    }

    private string playbackStatus = "Stopped";

    public string PlaybackStatus
    {
        get => playbackStatus;
        set
        {
            playbackStatus = value ?? throw new ArgumentNullException(nameof(value));
            EmitPropertyChanged(PlayerProperty.PlaybackStatus);
        }
    }

    private bool canGoNext;

    public bool CanGoNext
    {
        get => canGoNext;
        set
        {
            canGoNext = value;
            EmitPropertyChanged(PlayerProperty.CanGoNext);
        }
    }

    private bool canGoPrevious;

    public bool CanGoPrevious
    {
        get => canGoPrevious;
        set
        {
            canGoPrevious = value;
            EmitPropertyChanged(PlayerProperty.CanGoPrevious);
        }
    }

    private long Position => mbApiInterface.Player_GetPosition() * 1000;

    private Dictionary<string, VariantValue> metadata = new();

    public Dictionary<string, VariantValue> Metadata
    {
        get => metadata;
        set
        {
            metadata = value ?? throw new ArgumentNullException(nameof(Metadata));
            EmitPropertyChanged(PlayerProperty.Metadata);
        }
    }

    public async Task AddToDBusAsync()
    {
        await handler.AddToDBusAsync();
        emitSignals = true;
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
        connection.EmitSeeked(ObjectPath, (long)position * 1000);
    }

    private void Seek(long offset)
    {
        var currentPos = mbApiInterface.Player_GetPosition();
        var newPos = Math.Max(0, currentPos + (int)(offset / 1000));
        mbApiInterface.Player_SetPosition(newPos);
        OnSeeked(newPos);
    }

    private void SetPosition(ObjectPath trackId, long position)
    {
        var newPos = (int)(position / 1000);
        mbApiInterface.Player_SetPosition(newPos);
        OnSeeked(newPos);
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

    private void EmitPropertyChanged(PlayerProperty property)
    {
        if (!emitSignals)
        {
            return;
        }

        connection.EmitPropertyChanged(ObjectPath, handler, property);
    }

    private void EmitPropertyChanged(MediaPlayer2Property property)
    {
        if (!emitSignals)
        {
            return;
        }

        connection.EmitPropertyChanged(ObjectPath, handler, property);
    }

    private class DBusMediaPlayerHandler(DBusConnection connection, DBusMediaPlayer player)
        : DBusHandler(connection, ObjectPath, false), IMediaPlayer2Handler, IMediaPlayer2Properties, IPlayerHandler, IPlayerProperties
    {
        // IMediaPlayer2Properties
        public bool CanQuit => false;
        public bool CanRaise => false;
        public bool CanSetFullscreen => false;
        public bool HasTrackList => false;
        public string Identity => "MusicBee";
        public string DesktopEntry => "MusicBee";
        public string[] SupportedUriSchemes => ["file"];
        public string[] SupportedMimeTypes => [""];

        public bool Fullscreen
        {
            get => player.Fullscreen;
            set => player.Fullscreen = value;
        }

        // IPlayerProperties
        public bool CanControl => true;
        public double MinimumRate => 1.0;
        public double MaximumRate => 1.0;

        public bool CanGoNext => player.CanGoNext;

        public bool CanGoPrevious => player.CanGoPrevious;

        public bool CanPause => true;

        public bool CanPlay => true;

        public bool CanSeek => true;

        public string LoopStatus
        {
            get => player.LoopStatus;
            set => player.SetLoopStatus(value);
        }

        public Dictionary<string, VariantValue> Metadata => player.Metadata;

        public string PlaybackStatus => player.PlaybackStatus;

        public long Position => player.Position;

        public double Rate
        {
            get => player.Rate;
            set => player.Rate = value;
        }

        public bool Shuffle
        {
            get => player.Shuffle;
            set => player.SetShuffle(value);
        }

        public double Volume
        {
            get => player.Volume;
            set => player.SetVolume(value);
        }

        public async Task AddToDBusAsync()
        {
            Connection.AddMethodHandler(this);
            await Connection.RequestNameAsync(ServiceName);
        }

        // IMediaPlayer2Handler
        ValueTask IMediaPlayer2Handler.RaiseAsync()
        {
            return default;
        }

        ValueTask IMediaPlayer2Handler.QuitAsync()
        {
            return default;
        }

        // IPlayerHandler
        ValueTask IPlayerHandler.NextAsync()
        {
            player.Next();
            return default;
        }

        ValueTask IPlayerHandler.PreviousAsync()
        {
            player.Previous();
            return default;
        }

        ValueTask IPlayerHandler.PauseAsync()
        {
            player.Pause();
            return default;
        }

        ValueTask IPlayerHandler.PlayPauseAsync()
        {
            player.PlayPause();
            return default;
        }

        ValueTask IPlayerHandler.StopAsync()
        {
            player.Stop();
            return default;
        }

        ValueTask IPlayerHandler.PlayAsync()
        {
            player.Play();

            return default;
        }

        ValueTask IPlayerHandler.SeekAsync(long offset)
        {
            player.Seek(offset);
            return default;
        }

        ValueTask IPlayerHandler.SetPositionAsync(ObjectPath trackId, long pos)
        {
            player.SetPosition(trackId, pos);
            return default;
        }

        ValueTask IPlayerHandler.OpenUriAsync(string uri)
        {
            /* Unsupported function, MusicBee does not allow playing arbitrary files */
            return default;
        }

        // Property methods
        ValueTask IMediaPlayer2Handler.HandleGetAllPropertiesAsync(IMediaPlayer2Handler.GetAllPropertiesContext context) => context.Handle(this);

        ValueTask IMediaPlayer2Handler.HandleGetPropertyAsync(IMediaPlayer2Handler.GetPropertyContext context) => context.Handle(this);

        ValueTask IMediaPlayer2Handler.HandleSetPropertyAsync(IMediaPlayer2Handler.SetPropertyContext context) => context.Handle(this);

        ValueTask IPlayerHandler.HandleGetAllPropertiesAsync(IPlayerHandler.GetAllPropertiesContext context) => context.Handle(this);

        ValueTask IPlayerHandler.HandleGetPropertyAsync(IPlayerHandler.GetPropertyContext context) => context.Handle(this);

        ValueTask IPlayerHandler.HandleSetPropertyAsync(IPlayerHandler.SetPropertyContext context) => context.Handle(this);
    }
}