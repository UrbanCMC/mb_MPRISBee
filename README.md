# mb_MPRISBee plugin
A MusicBee plugin that sends player status to [mprisbee-bridge](https://github.com/Kyletsit/mprisbee-bridge) for MPRIS integration. Setup instructions are in that repo's README.

### Changes in this fork
* Fixed a typo that caused a `Play` event to be sent when MusicBee finishes playback, instead of `Stop`.
* Replaced native linux syscalls for socket reading/writing with winsock's AF_UNIX sockets.
  * The native syscalls were broken by `wine` update 11.5.
  * As of writing (2026-03-31), AF_UNIX is only supported on `wine-staging` 10.2 or later.
* Errors are now also logged to the WINEPREFIX's %APPDATA%\MusicBee\MPrisBee\mb_MPrisBee.log

### Credits
The idea to call syscalls in raw assembly is from [wine-discord-ipc-bridge](https://github.com/0e4ef622/wine-discord-ipc-bridge)

The code for putting assemblies into a subfolder is from [DiscordBee](https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs)
