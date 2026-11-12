# Dark Nebula Rebuild

[简体中文](README.zh-CN.md)

A Unity 6 restoration of **Dark Nebula Episode One** and **Episode Two** for modern mobile devices, combining content recovered from the iOS and Android releases.

## Project status

The current verification and regression work is complete. All currently identified bugs inherited from the original games or introduced during restoration have been fixed. Active development is on hold unless a major bug or missing feature is found. Please report problems through [GitHub Issues](https://github.com/FluorescentLava/dark-nebula-rebuild/issues), ideally with your device, system version, steps to reproduce, and screenshots or recordings.

## Features

### Core features

- [x] Modern Android support and tall-screen portrait layouts.
- [x] Restored levels, gameplay, artwork and effects from both episodes.
- [x] Episode Two's generated portal scenery shares the native bridge layer and stays aligned as the camera rises and rotates.
- [x] Local progress, level ratings and leaderboards.
- [x] Level completion results with skippable scoring animations.
- [x] Restored Episode Two intro and extro movies with their original soundtracks and two-step skip controls. A first tap shows a gray prompt in the game's sci-fi font; release, then tap the prompt to skip. The intro plays on the first main-menu entry. New Game starts the first level without replaying it. Credits plays the extro and returns to the main menu, while completing the campaign plays the extro before EndOfGame.
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
- [x] Manual calibration keeps the completion screen open until the player chooses Go Back.
- [x] WASD controls for Windows Unity debugging, excluded from Release builds.

## Build from source

Download **Source code (zip)** or **Source code (tar.gz)** from the selected GitHub release, or check out its tag. GitHub generates these archives from the release tag; uploaded release assets contain the two Android APKs and two unsigned iOS IPAs. Open `EpisodeOne` or `EpisodeTwo` as a separate project in Unity **6000.4.11f1**, with the relevant Android or iOS Build Support module installed. Local caches, build products, validation tooling and signing keys are excluded from the source archive.

For Android, the editor entry points are `BuildEpisodeOne.Build` and `BuildEpisodeTwo.Build` under each project's `Assets/Editor`. They target ARM64 and Android API 26 or newer, with target API 35. Configure Android SDK, NDK **27.2.12479018** and JDK **17**; the build entry points accept the JDK path through `DARK_NEBULA_JDK17`. Android distribution signing uses your own keystore.

For iOS, switch the chosen project to iOS and export an Xcode project. The repository's manual **Build iOS** GitHub Actions workflow shows the Unity export, Xcode Release compilation and unsigned IPA packaging steps. Building requires macOS and Xcode; device installation additionally requires Apple signing and provisioning.

## License

[MIT](LICENSE) for original rebuild contributions. Recovered content and third-party components retain their respective rights; see [NOTICE](NOTICE.md).
