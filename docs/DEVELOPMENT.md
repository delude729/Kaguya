# 开发与构建说明

面向需要修改、重新构建或验证本项目的开发者。普通使用请回到 [README](../README.md)。

## 构建

| 命令 | 作用 |
| --- | --- |
| `build.cmd` | 生成 `assets/kaguya.ico`，用 Windows 自带的 .NET Framework 编译器编译 `src/*.cs`，再检查 EXE 与 Shell 图标 |
| `RunKaguya.cmd` | 启动 `KaguyaPet.exe`；资产版需要与 `assets` 文件夹同级 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-single-file.ps1` | 生成单文件版，输出到 `单文件发布版/KaguyaPet.exe` |
| `KaguyaPet.exe --self-test` | 运行自动验证，结果写入 `verification/self-test.json` |

修改 C# 后请先退出正在运行的桌宠，再执行构建。

## 项目结构

| 路径 | 说明 |
| --- | --- |
| `src/` | `Kaguya.cs` 窗口与交互、`Native.cs` Win32 API、`Appearance.cs` 原生绘制、`Checks.cs` 自动验证、`Pet.manifest` 应用程序清单 |
| `assets/` | 角色图片、图标、光标、各 DPI 面板素材 |
| `tools/` | 图标生成、单文件打包、UI 素材导出、光标提取与验证脚本 |
| `reference-ui/` | 面板的原始 HTML/CSS 设计参考，仅在重新导出素材时使用 |
| `verification/original-geometry.json` | 自检和单文件打包使用的几何基准 |

构建产物（`KaguyaPet.exe`、`单文件发布版/`）以及 `verification/` 下的截图、日志、旧 EXE 均被 `.gitignore` 排除。

## DPI 素材

`assets/ui` 是 100% 缩放素材；`assets/ui/dpi-125` 到 `dpi-300` 对应更高显示缩放。C# 启动时按当前屏幕 DPI 选择目录，并把对应位图按物理像素 1:1 合成，避免低分辨率素材被二次放大。

`assets/ui/layout.json` 保存面板各元素在逻辑坐标中的位置；`badge-labels.json` 保存底部状态栏的原始 CSS 字号、位置和文案，供 C# 在最终物理像素画布上重新对齐绘制。

角色支持 50%～120% 的连续滑块，步进 1%，显示当前百分比。拖动捕获鼠标、实时预览，松手或取消捕获时保存；面板打开后位置固定，防止缩放时滑块移动。滑块获得键盘焦点时，方向键调整 1%，Home/End 跳到范围两端。旧档位存档继续兼容，非有限数值回退 100%，超范围数值限制到有效范围。`reference-ui` 保留原始置顶行和“安静地，陪着你”文案，喝水设置仅放在托盘右键菜单中。导出时为动态滑块和百分比保留空位，运行时按模板导出的几何坐标及最终 DPI 绘制动态内容，无需用底色覆盖旧控件。

所有比例的角色拖至工作区最右侧时，参考人物轮廓到工作区右边缘统一保留 44.125 个逻辑像素（沿用原 1.2× 档位）。左、上、下边界维持原规则；打开面板仍会单独确保整个窗口位于工作区内。

重新生成素材：

```bat
:: 需要单独安装 Electron 运行时
electron tools\export-ui.cjs --ui-scale=1.25
```

`tools/export-ui.cjs` 会加载 `reference-ui/index.html`，模拟休息、置顶和角色大小的各种状态，然后截图并写入 `assets/ui/dpi-125`。100% 不传 `--ui-scale` 或传 `--ui-scale=1`，输出到 `assets/ui`。`--badge-only` 只更新状态栏素材。

`reference-ui/` 是构建期模板，不是可直接运行的应用：页面引用的 `renderer.js` 和角色图片不在该目录中。直接打开会出现 404，但导出脚本会自行展开面板、隐藏角色再截图，因此不影响素材生成。

## 运行时

- 透明窗口使用 Win32 `UpdateLayeredWindow`，与 Naiwa 的透明窗口路径一致。
- 角色五张 PNG 在启动时解码，动作切换不读磁盘；喝水提醒使用 `water-offer.png`，复用现有气泡和角色缩放逻辑。`water-sip.png` 暂未使用、不随启动加载，保留给后续动作设计。
- 鼠标移动直接更新窗口位置，不通过浏览器进程通信或轮询追踪。
- 存档写入 `%APPDATA%\kaguya-native-pet\pet-state.json`。
- 仅从 `%APPDATA%\kaguya-native-pet\pet-state.json` 读取存档；没有存档时使用默认设置。
- 新存档使用 `energy` 字段；旧 `milk` 字段只用于兼容读取。
- 喝水设置保存为 `waterEnabled`（默认 false）、`waterMinutes`（默认 30，范围 1～1440）。以进程单调时钟计时，启动或修改设置时重置截止时间；到期暂缓期间不积累提醒队列。休息状态照常提醒；隐藏时到期会重新显示角色并提醒。拖动、编辑间隔、菜单或正在对话时等待，之后只触发一次，直接递杯动作持续 4 秒，手动互动可中断。
- 托盘右键菜单提供“喝水提醒”开关和“提醒间隔”设置。间隔使用 ToolStripControlHost 内嵌输入框，点击数字时激活菜单并获取键盘焦点；回车或失焦/关闭菜单保存有效值，Esc 取消，非法值不覆盖原配置。普通菜单操作维持不抢焦点。面板不包含喝水控件。

## 验证

`KaguyaPet.exe --self-test` 覆盖按人物实际像素轮廓命中、三档大小和四种动作的左右及头顶空白鼠标穿透、Windows 对透明区域的窗口路由、三档四向边界、5px 拖动阈值、动作与数值规则、置顶、隐藏找回、休息/隐藏期间的喝水提醒，以及连续切换动作的非空画面和 GDI 句柄稳定性。截图和结果写入 `verification/`（不纳入 Git）。

自动检查不能代替真实显示器上的拖动、缩放和闪烁观察。

## 其他工具

- `tools/extract-original-cursors.ps1 -ElectronExe <path>`：从原 Electron 可执行文件中提取光标资源，输出到 `verification/original-cursors`。
- `tools/build-kaguya-icon.ps1`：从 `assets/kaguya/idle.png` 生成 16–256px 的多尺寸 ICO。
- `tools/verify-kaguya-icon.ps1`：检查 EXE 与 Shell 图标非空，结果写入 `verification/icon-check.json`。
