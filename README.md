# Dark Nebula Rebuild

[简体中文](README.zh-CN.md)

A Unity 6 restoration of **Dark Nebula Episode One** and **Episode Two** for modern mobile devices, combining content recovered from the iOS and Android releases.

## Features (0.4.0-Beta)

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
- [x] WASD controls for Windows Unity debugging, excluded from Release builds.

## 0.4.0-Beta update

Episode One’s Episode Two button opens the installed app on Android and iOS, or opens this repository’s [Releases page](https://github.com/FluorescentLava/dark-nebula-rebuild/releases) when it cannot launch Episode Two. The iOS app registers its launch URL scheme. Both iOS exports now assign each episode’s original artwork to every supported application-icon slot, including iPhone, iPad and the App Store icon.

Both iOS apps hide the status bar. Episode Two disables and dims Continue when there is no level progress, including after the first intro and after returning from settings. New Game starts the first level directly without replaying the intro; existing progress still requires confirmation before it is reset.

Enhanced score feedback now follows the actual number animations in both episodes. Short pulses continue while counting, stop when counting ends or is skipped, and preserve higher-priority star feedback. iOS uses light native impacts for these score pulses.

## 0.3.7 update

Episode One's leaderboard retains its portraits and reuses Episode Two's time, date, English header font and star artwork, matching their sizes, colors and placement. Text anchors now use precise font metrics and visible glyph bounds across both languages, replacing the separate nickname offset correction. The completion screen now accepts and saves a nickname before adding that run to the local Top 10. A new local achievements browser shows all fourteen original gold-star milestones, badges and unlock progress in English and Simplified Chinese.

Episode One's settings and preparation screens now open the same vessel page and controller, combining the original swipe preview, shard artwork, purchases and equipment, including while paused. Its original shard balance panel follows the page layout. Episode Two displays its shard icon and balance without that panel at the upper left of the page's content, moving with the other controls instead of the screen edge. Shard pickups display a complete reward sentence. Episode One restores the message scale before aligning its text on both the first use and subsequent reuse; both episodes release expired messages exactly once so repeated pickups keep displaying correctly.

Episode Two's leaderboard now shares Episode One's original portraits and automatically shrinks long nicknames to fit beside the time. Complete Latin, Chinese and mixed names remain visible, and short names recover their normal size when rows are reused. Copied menu buttons retain their input hit areas, and remaining-life substitutions work in English. The temple transition restores the earlier upper canopy padding and forest-cap blend while retaining the current lower scenery patches and native camera motion. Enhanced Android vibration explicitly declares game usage, so short pickup effects are no longer misclassified as system touch feedback.

Both episodes reuse their main-menu settings logic in game, with translucent black backgrounds for pause, settings, calibration and confirmation screens. Calibration keeps a large heading and normal body text. Loading and timed angle tutorials consume back keys and edge gestures until their transitions complete. Pages sharing menu artwork now match its size, position, edge fades and black borders. Result nicknames fade in with the final scoring stage, leaderboard pages show large headings, and rating hints no longer overlap the nickname field. Chinese HUD messages share the English gradient and lower-left shadow without clipping. Episode One also fixes tall-screen mechanism wake and speed-change discontinuities.

Episode One restores its original loading-hint layout: all five illustrations appear on the left, with left-aligned text in the original column on the right. Legacy online-service copy now describes local features: the achievements hint directs players to all fourteen milestones and their unlock progress in Achievements; the others explain automatic saving, shard-based vessel unlocks, local leaderboards and two-finger calibration. The layout remains consistent across language changes and repeated hints. Upgrade badges now translate “UNLOCK” into Chinese and center its visible glyphs on the badge.

## 0.3.6 update

Episode Two's moving and rotating mechanisms now keep their position when checkpoint difficulty changes, fixing spikes that jumped back along their path while visible on tall screens. Sleeping sine obstacles restore their update and physics state correctly. Music imports are validated before Android packaging, portal scenery uses two small masked crops attached to the native terrain, and shockwave scans complete outside the tall viewport.

The iOS packages are unsigned and require an Apple-issued signing certificate and provisioning profile before installation on a regular iPhone or iPad.

iOS release filenames use `DarkNebula-Episode1-<version>-iOS.ipa` and `DarkNebula-Episode2-<version>-iOS.ipa`, without an `unsigned` suffix. Signing status is described in the release notes.

## Build from source

Download **Source code (zip)** or **Source code (tar.gz)** from the selected GitHub release, or check out its tag. GitHub generates these archives from the release tag; uploaded release assets contain the two Android APKs and two unsigned iOS IPAs. Open `EpisodeOne` or `EpisodeTwo` as a separate project in Unity **6000.4.11f1**, with the relevant Android or iOS Build Support module installed. Local caches, build products, validation tooling and signing keys are excluded from the source archive.

For Android, the editor entry points are `BuildEpisodeOne.Build` and `BuildEpisodeTwo.Build` under each project's `Assets/Editor`. They target ARM64 and Android API 26 or newer, with target API 35. Configure Android SDK, NDK **27.2.12479018** and JDK **17**; the build entry points accept the JDK path through `DARK_NEBULA_JDK17`. Android distribution signing uses your own keystore.

For iOS, switch the chosen project to iOS and export an Xcode project. The repository's manual **Build iOS** GitHub Actions workflow shows the Unity export, Xcode Release compilation and unsigned IPA packaging steps. Building requires macOS and Xcode; device installation additionally requires Apple signing and provisioning.

## License

[MIT](LICENSE) for original rebuild contributions. Recovered content and third-party components retain their respective rights; see [NOTICE](NOTICE.md).
