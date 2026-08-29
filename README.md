# mb_MPRISBee plugin
A MusicBee plugin that exposes the player status via a DBUS MPRIS integration. To install, simply place the build output in MusicBee's `Plugins` directory.

### Changes in this fork
* Fixed a typo that caused a `Play` event to be sent when MusicBee finishes playback, instead of `Stop`.
* Directly exposes an MPRIS player handler to DBUS, instead of relying on a bridge process running on the Linux side.
  * The native syscalls stopped working with `wine` update 11.5.
  * As of writing (2026-03-31), AF_UNIX socket communication is only supported on `wine-staging` 10.2 or later.
* Errors are now also logged to the WINEPREFIX's %APPDATA%\MusicBee\MPRISBee\mb_MPRISBee.log

### A Note for Bottles/Flatpak users
If you're running MusicBee in a Bottles environment, you're most likely running it through Flatpak.  
In this case, you need to allow Bottles to register the session bus name that MPRISBee will be using:
```sh
flatpak override --user --own-name=org.mpris.MediaPlayer2.MusicBee com.usebottles.bottles
```

Additionally, because of how Flatpak sandboxes the file system, the URL for the currently playing track will
follow this schema in your metadata: `file:///run/flatpak/doc/UjyaZyN8GVjqi24Xz8txnA/[...]`  
This is unlikely to be an issue for most people, but if it is for you, I'd suggest not using Flatpak.

### Credits
The code for putting assemblies into a subfolder is from [DiscordBee](https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs)
