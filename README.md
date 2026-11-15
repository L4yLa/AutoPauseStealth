# AutoPauseStealth

Fork of [Aryetis/AutoPauseStealth](https://github.com/Aryetis/AutoPauseStealth).

Invisibly pauses the game at the start of every song until the frame rate is stable,
so startup lag spikes don't cost you notes.

## Settings

Mod Settings > AutoPauseStealth

- **Top line**: FPS since the page was opened, and your headset refresh rate.
- **Target FPS**: usually your headset refresh rate. Choose **Manual** to set **Min FPS** directly.
- **Threshold**: how close to Target FPS every frame must be (Strict 97% / Normal 95% / Loose 90%).
- **FPS Stability Check Duration**: how long the FPS must stay above the threshold before the song starts.
- **Force GameStart duration**: the song starts after this time even if the FPS never stabilizes.
- **Reload if fps stabilization failed**: reloads the song instead of starting it.

If every song waits the full Force GameStart duration, lower Target FPS or use a looser Threshold.

Has no effect in multiplayer.

## Installation

Install BSIPA and BeatSaberMarkupLanguage, then extract the release archive into your Beat Saber folder.

## License

MIT. Original work by Aryetis.
