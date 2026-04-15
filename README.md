# mb_MPRISBee plugin
A MusicBee plugin that exposes the player status via a DBUS MPRIS integration. To install, simply place the build output in MusicBee's `Plugins` directory.

### Changes in this fork
* Fixed a typo that caused a `Play` event to be sent when MusicBee finishes playback, instead of `Stop`.
* Directly exposes an MPRIS player handler to DBUS, instead of relying on a bridge process running on the Linux side.
  * The native syscalls stopped working with `wine` update 11.5.
  * As of writing (2026-03-31), AF_UNIX socket communication is only supported on `wine-staging` 10.2 or later.
* Errors are now also logged to the WINEPREFIX's %APPDATA%\MusicBee\MPRISBee\mb_MPRISBee.log

### Credits
The code for putting assemblies into a subfolder is from [DiscordBee](https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs)
