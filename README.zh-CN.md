# Dark Nebula Rebuild

[English](README.md)

基于 Unity 6 的 **Dark Nebula Episode One** 与 **Episode Two** 复原项目，整合 iOS 和 Android 版本内容，适配现代移动设备。

## 功能（0.3.3）

### 核心功能

- [x] 现代 Android 支持与竖屏高屏适配。
- [x] 两代关卡、玩法、美术和特效复原。
- [x] 二代石阵生成补景直接显示，不使用透明度羽化或场景交叉渐变，已通过生产流程桌面验证。
- [x] 本地进度、关卡评级与排行榜。
- [x] 关卡结算与可跳过的计分动画。
- [x] 恢复二代原版 intro 与 extro 影片、音轨和跳过操作。首次进入主菜单播放 intro；主菜单 Credits 播放 extro 后返回主菜单，通关时播放 extro 后进入 EndOfGame。
- [x] 两代 iOS Unity 导出、Xcode 编译与未签名 IPA 打包技术链路已验证。
- [ ] 本次 0.3.3 最终 iOS 构建的真机安装与玩法验证。

### 拓展功能

- [x] 沿用原版设置界面，扩展计时器、语言、震动与选球控制，调整间距并整体居中。
- [x] 两代可选关卡计时器，默认开启。
- [x] 两代选球功能，通过免费收集碎片购买解锁。
- [x] 英语与简体中文，内置几何风格中文字体，调整字形大小、全角字宽与标签居中。
- [x] 恢复二代设置与排行榜中文选项切换时的字距动画，保留原有字形、静止布局和颜色。
- [x] 两代确认框背景铺满长屏视图。
- [x] 中文昵称输入与排行榜显示，采用匹配原版风格的黑体。
- [x] 高质量音频解码与位置立体声效。
- [x] 关闭、原版、增强三种震动模式，默认增强。
- [x] 增强震动包含跳板多段衰减、普通道具轻震、碎片与 1UP 中震，以及不被较弱事件打断的 650 毫秒死亡反馈。
- [x] 每次关卡开始前完成后台传感器校准。
- [x] Windows Unity 调试用 WASD 控制，Release 不包含。

## 0.3.3 构建状态

二代石阵的最终方案为只保留生成补景，取消额外场景过渡与透明度羽化；补景生产流程桌面检查和影片流程回归已通过。发布安装包必须从本次验收通过的源码提交构建；此前的 0.3.3 APK 与 CI 导出仍为开发候选产物。本次最终修订尚未在 Android 或 iOS 真机上测试。iOS 验证仅覆盖技术导出、Xcode 编译与包结构；IPA 为未签名包，需要 Apple 签发的签名证书与描述文件，才能安装到普通 iPhone 或 iPad。

## 从源码构建

使用 Unity **6000.4.11f1** 分别打开 `EpisodeOne` 或 `EpisodeTwo` 工程，并安装所需的 Android 或 iOS Build Support 模块。源码 ZIP 对应发布提交中的受版本控制文件，不包含本地缓存、构建产物、验证工具或签名密钥。

Android 构建入口为各工程 `Assets/Editor` 下的 `BuildEpisodeOne.Build` 和 `BuildEpisodeTwo.Build`，目标为 ARM64，最低 Android API 26、目标 API 35。需配置 Android SDK、NDK **27.2.12479018** 与 JDK **17**；构建入口可通过 `DARK_NEBULA_JDK17` 读取 JDK 路径。自行分发 Android 包时需使用自己的签名密钥库。

iOS 构建需将对应工程切换到 iOS 并导出 Xcode 工程。仓库的手动 **Build iOS** GitHub Actions 工作流包含 Unity 导出、Xcode Release 编译和未签名 IPA 打包步骤。构建需要 macOS 与 Xcode；真机安装还需要 Apple 签名和描述文件。

## 许可

原创复原代码使用 [MIT 许可证](LICENSE)。复原内容及第三方组件保留各自权利，详见 [NOTICE](NOTICE.md)。
