# Dark Nebula Rebuild

[English](README.md)

基于 Unity 6 的 **Dark Nebula Episode One** 与 **Episode Two** 复原项目，整合 iOS 和 Android 版本内容，适配现代移动设备。

## 功能（0.3.6）

### 核心功能

- [x] 现代 Android 支持与竖屏高屏适配。
- [x] 两代关卡、玩法、美术和特效复原。
- [x] 二代石阵生成补景与原版桥面处于同一图层，在镜头升降和旋转时保持贴合。
- [x] 本地进度、关卡评级与排行榜。
- [x] 关卡结算与可跳过的计分动画。
- [x] 恢复二代原版 intro 与 extro 影片、音轨和两步跳过操作。首次点击显示游戏科幻字体的灰色提示，松开后再点击提示区域即可跳过。首次进入主菜单，以及每次确认开始新游戏后，都会播放 intro；新游戏在 intro 结束后进入第一关。主菜单 Credits 播放 extro 后返回主菜单，通关时播放 extro 后进入 EndOfGame。
- [x] 两代 iOS Unity 导出、Xcode 编译与未签名 IPA 打包技术链路已验证。

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

## 0.3.6 更新

二代移动与旋转机关在检查点调整难度时保持当前位置，修复高屏下方仍可见的尖刺突然跳回运动路径另一端的问题。正弦障碍休眠后能完整恢复更新和物理状态。Android 打包前校验音乐导入资源，石阵补景改用跟随原生地形的两块小型遮罩贴图，扫描光带在高屏可视范围外完成运动。

iOS 提供未签名 IPA，需要 Apple 签发的签名证书与描述文件，才能安装到普通 iPhone 或 iPad。

## 从源码构建

从选定的 GitHub Release 下载 **Source code (zip)** 或 **Source code (tar.gz)**，也可直接检出对应标签。GitHub 会按发布标签自动生成源码归档；额外上传的发布资产为两代 Android APK 和两代未签名 iOS IPA。使用 Unity **6000.4.11f1** 分别打开 `EpisodeOne` 或 `EpisodeTwo` 工程，并安装所需的 Android 或 iOS Build Support 模块。源码归档不包含本地缓存、构建产物、验证工具或签名密钥。

Android 构建入口为各工程 `Assets/Editor` 下的 `BuildEpisodeOne.Build` 和 `BuildEpisodeTwo.Build`，目标为 ARM64，最低 Android API 26、目标 API 35。需配置 Android SDK、NDK **27.2.12479018** 与 JDK **17**；构建入口可通过 `DARK_NEBULA_JDK17` 读取 JDK 路径。自行分发 Android 包时需使用自己的签名密钥库。

iOS 构建需将对应工程切换到 iOS 并导出 Xcode 工程。仓库的手动 **Build iOS** GitHub Actions 工作流包含 Unity 导出、Xcode Release 编译和未签名 IPA 打包步骤。构建需要 macOS 与 Xcode；真机安装还需要 Apple 签名和描述文件。

## 许可

原创复原代码使用 [MIT 许可证](LICENSE)。复原内容及第三方组件保留各自权利，详见 [NOTICE](NOTICE.md)。
