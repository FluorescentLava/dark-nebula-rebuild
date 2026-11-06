# Dark Nebula Rebuild

[简体中文](README.zh-CN.md)

A Unity 6 restoration of **Dark Nebula Episode One** and **Episode Two** for modern mobile devices, combining content recovered from the iOS and Android releases.

## Features (0.3.4)

### Core features

- [x] Modern Android support and tall-screen portrait layouts.
- [x] Restored levels, gameplay, artwork and effects from both episodes.
- [x] Episode Two's generated portal scenery shares the native bridge layer and stays aligned as the camera rises and rotates.
- [x] Local progress, level ratings and leaderboards.
- [x] Level completion results with skippable scoring animations.
- [x] Restored Episode Two intro and extro movies with their original soundtracks and two-step skip controls. A first tap shows a gray prompt in the game's sci-fi font; release, then tap the prompt to skip. The intro plays on the first main-menu entry and whenever New Game is accepted, before the first level starts. Credits plays the extro and returns to the main menu, while completing the campaign plays the extro before EndOfGame.
- [x] iOS Unity export, Xcode compilation and unsigned IPA packaging verified for both episodes.

### Extensions

- [x] Original settings interface with timer, language, vibration and vessel controls in compact, centered layouts.
- [x] Optional level timer in both episodes, enabled by default.
- [x] Vessel selection in both episodes with shard purchases and free in-level collection.
- [x] English and Simplified Chinese with a bundled geometric Chinese font, adjusted glyph sizing, fullwidth character widths and centered labels.
- [x] Restored letter-spacing animations when switching Chinese options in Episode Two's settings and leaderboards, preserving the existing glyph shapes, resting layout and colors.
- [x] Confirmation-dialog backgrounds cover the full tall-screen view in both episodes.
- [x] Chinese nickname input and leaderboard display with matching sans-serif fonts.
- [x] High-quality audio decoding and positional stereo effects.
- [x] Vibration modes: disabled, original and enhanced; enhanced by default.
- [x] Enhanced vibration with decaying multi-pulse jump-pad feedback, light ordinary-pickup and medium shard/1UP feedback, and 650 ms death feedback protected from weaker events.
- [x] Background sensor calibration before each level starts.
- [x] WASD controls for Windows Unity debugging, excluded from Release builds.

## 0.3.4 update

Episode Two's portal scenery follows the bridge's actual layer, keeping the fill attached to the road throughout the camera movement. Story movies use two-step skip confirmation with a smaller, raised gray prompt in the existing geometric Chinese and BankGothic menu fonts. Every accepted New Game replays the intro before starting the first level; cancelling the confirmation preserves progress, and Continue keeps its existing route. The 0.3.3 release remains available separately.

The iOS packages are unsigned and require an Apple-issued signing certificate and provisioning profile before installation on a regular iPhone or iPad.

## Build from source

Open `EpisodeOne` or `EpisodeTwo` as a separate project in Unity **6000.4.11f1**, with the relevant Android or iOS Build Support module installed. The source ZIP contains the tracked repository at the release commit; local caches, build products, validation tooling and signing keys are excluded.

For Android, the editor entry points are `BuildEpisodeOne.Build` and `BuildEpisodeTwo.Build` under each project's `Assets/Editor`. They target ARM64 and Android API 26 or newer, with target API 35. Configure Android SDK, NDK **27.2.12479018** and JDK **17**; the build entry points accept the JDK path through `DARK_NEBULA_JDK17`. Android distribution signing uses your own keystore.

For iOS, switch the chosen project to iOS and export an Xcode project. The repository's manual **Build iOS** GitHub Actions workflow shows the Unity export, Xcode Release compilation and unsigned IPA packaging steps. Building requires macOS and Xcode; device installation additionally requires Apple signing and provisioning.

## License

[MIT](LICENSE) for original rebuild contributions. Recovered content and third-party components retain their respective rights; see [NOTICE](NOTICE.md).
