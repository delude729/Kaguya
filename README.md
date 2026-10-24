# 辉夜桌宠 · Windows 原生版

Windows 桌面宠物，使用 C# / .NET Framework / WinForms 原生实现透明窗口与交互，不依赖 Electron 或 Node.js。

**[下载最新单文件版](../../releases/latest)**：只需一个 `KaguyaPet.exe`，图片、面板、光标等资源均已嵌入，不需要额外的 `assets` 文件夹。

光标现与旧版 CSS `grab` / `:active grabbing` 对齐：直接使用与旧版 Electron 内置资源字节一致的 CUR 文件，通过 WinForms 标准 Cursor 加载，不额外缩放。悬停为抓手，鼠标按下立即握拳，松开恢复；不再等到跨过拖动阈值才变化。

右键体验更新：使用原版 Chromium 手形光标资源（assets/cursors）；菜单左下角贴近点击位置，向上展开，靠近屏幕边缘时自动限制在工作区内。菜单窗口复用并在启动时预热；菜单可见时暂停角色背景重绘。角色与菜单均采用不激活、不进入任务栏的工具窗口样式，避免右键引出 WinForms 辅助窗口的任务栏图标。已有面板开关、置顶记忆、拖动区域与边界保持不变。

置顶设置：第一次启动默认开启始终置顶；之后启动读取上次退出前的选择，开、关都会记住。

右键菜单中的“启用面板”开关默认开启。关闭会立即收起面板，之后点击底部状态栏不再响应；角色拖动、摸头和右键菜单仍可使用。重新开启即可恢复。此开关也会保存，下次启动继续使用上次的选择。菜单中的粉色开关代表开启，灰色代表关闭。

2026-10-24 外观更新：角色悬停使用抓手光标，实际拖动使用握拳光标；右键菜单采用白底圆角、居中文字、统一行距和分隔线，保持原菜单功能与顺序。相关原生绘制实现在 src/Appearance.cs。

## 构建与运行

运行 `build.cmd`，使用 Windows 自带的 .NET Framework 编译器生成 `KaguyaPet.exe`，然后双击 **RunKaguya.cmd**（或直接运行该 EXE）。此版本运行时需要 exe 与 `assets` 文件夹放在一起。无需 Node.js、Electron 或额外安装开发工具。

单文件发布版可从 [Releases](../../releases/latest) 下载 `KaguyaPet.exe`：只需发送这一个 EXE，图片、面板、光标及相应资源均已嵌入，不需要旁边的 assets 文件夹，也不会在启动时解压素材。功能、外观、数值规则与存档位置沿用原版。需要在本地重新构建单文件版时，运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/build-single-file.ps1`；生成的中间源码保存在 `verification/single-file-build`，发布目录 `单文件发布版` 仅放 EXE，两者均不纳入 Git。

## 仓库结构

- `src/`：C# 源码与应用程序清单。
- `assets/`：角色图片、图标、光标及各 DPI 的面板素材。
- `tools/`：图标生成、单文件打包、UI 素材导出与验证脚本。
- `reference-ui/`：面板的原始 HTML/CSS 视觉参考。
- `verification/original-geometry.json`：自动验证使用的原始几何基准。
- `build.cmd` / `RunKaguya.cmd`：编译与启动脚本。

构建产物（`KaguyaPet.exe`、`单文件发布版/`）和自动验证生成的截图、日志不纳入版本控制。

## 已保留的行为

- 单击角色摸摸头；拖动角色搬家；右键显示原有菜单。
- 原 290×300 的矩形可拖动区域保留，透明留白也可以抓取；移动超过 5 个逻辑像素才开始拖动。
- 同一套茶会面板配色、圆角、标题、按钮布局及文案；喝茶、摸头、睡眠、置顶、隐藏、小中大尺寸和托盘找回。
- 小、中、大仍为 0.8、1、1.2，移动边界沿用原 main.cjs 的标定公式；打开面板时先移入工作区，拖动开始时收起面板。
- 元气每 30 秒减少 1；清醒时心情每 30 秒减少 1，休息时心情每 30 秒增加 1。两个数值均限制在 0–100。喝茶元气 +20、心情 +5，摸头心情 +8。休息时不能喝茶或摸头。隐藏角色或关闭面板仍然计时，退出程序后不计算离线变化。
- 气泡 3.3 秒、动作图片 3.2 秒；呼吸、摸头摇摆、拖动倾斜及睡眠标记迁为原生绘制。

## 实现

2026-10-24 图标与命名更新：EXE、窗口与托盘统一使用 `assets/kaguya/idle.png` 生成的透明图标，保持原比例，内含 16–256px 多尺寸。`build.cmd` 自动重新生成 `assets/kaguya.ico`。当前程序标识统一为 Kaguya，元气字段与 UI 标识统一为 `energy`；旧 `milk` 字段及旧存档目录仅用于兼容读取，新存档只写入 `energy`。

C# / .NET Framework / WinForms / GDI+ / Win32 UpdateLayeredWindow，与 Naiwa 使用相同透明窗口路径。Native.cs 的分层位图表面实现取自用户提供的 Naiwa 源码。角色四张 PNG 在启动时解码，不在动作切换时读取磁盘。鼠标移动直接更新窗口位置，不通过浏览器进程通信或定时轮询追踪。

为保留原面板设计，构建阶段从原 HTML/CSS 按 100%、125%、150%、175%、200%、250%、300% 显示缩放导出面板和状态栏；启动时选取对应 DPI 素材，并以物理像素 1:1 合成，避免把低分辨率文字再次放大。非预设缩放优先选较高分辨率素材，仅在启动加载时缩小一次。数值及悬停文字直接按屏幕 DPI 绘制，命中测试和逻辑仍使用原坐标。原始视觉参考保存在 reference-ui。

原 pet 文件夹和 Naiwa 源码未修改。首次运行读取旧 `%APPDATA%\naiwa-desktop-pet\pet-state.json` 中的心情、元气、休息、置顶和大小，之后独立保存到 `%APPDATA%\kaguya-native-pet\pet-state.json`。与原版一致，启动位置为右下角。请先退出旧版再比较拖动体验，避免两个角色重叠。

## 源码与验证

- `src/Kaguya.cs`：原生窗口、完整交互、原尺寸/边界计算及绘图。
- `src/Native.cs`：Windows API 与可复用透明绘图表面。
- `src/Checks.cs`：自动验证；运行 `KaguyaPet.exe --self-test`，结果写入本地 `verification/self-test.json`（不纳入 Git）。
- `build.cmd`：使用 Windows 自带 Framework 编译器编译。修改 C# 后先退出原生版，再双击构建。
- `tools/export-ui.cjs`：仅重新生成视觉资源时需要 Electron；正常运行及 C# 编译不需要。参考页面已保留在本目录。
- 导出高 DPI 素材示例：以 Electron 运行 `tools/export-ui.cjs --ui-scale=1.25`，输出至 `assets/ui/dpi-125`。验证过程中的对比截图只在本地生成，不纳入 Git。
- 底部状态栏将背景和文字分开处理：`badge-labels.json` 保留原 CSS 字号、位置、内容；C# 在最终物理像素画布上对齐并绘制“辉夜 / 在你身边 / 休息中”，避免 9px 小字的半像素居中模糊。导出参数 `--badge-only` 可只更新状态栏素材。

验证覆盖原命中区域、Windows 鼠标穿透返回值、三档四向边界、5px 阈值、直接拖动、动作/数值规则、置顶、隐藏找回，以及连续切换动作的非空画面和 GDI 句柄稳定性。验证输出保存在本地 `verification`（不纳入 Git）。自动检查不能代替真实显示器上试拖动与观察闪烁。

临时自由拖动标定仍支持在新用户数据目录的 movement-calibration.json 写入 `{"enabled":true}`，重启后生效；每次拖动结束生成 movement-calibration-position.json。正常使用无需配置。

As of 2026-10-24
