<div align="center">

# 辉夜桌宠

把四宫辉夜、早坂爱和藤原请到你的 Windows 桌面上。

她们会陪你喝茶、打瞌睡，也会在你忙碌的时候安静地待在屏幕一角。

[**⬇️ 下载最新版 · KaguyaPet.exe**](../../releases/latest) · [查看所有版本](../../releases)

<sub>Windows 10 / 11 · 单文件免安装 · 完全离线</sub>

</div>

<p align="center">
  <img src="docs/characters.png" width="860" alt="可以随时切换的三位角色：四宫辉夜、早坂爱、藤原">
</p>

## 快速开始

1. 打开 [Releases](../../releases/latest)，下载最新的 `KaguyaPet.exe`。
2. 双击运行，默认出现辉夜，她会待在屏幕右下角。
3. 单击摸摸头，按住拖动搬家，点底部的状态栏打开茶会面板。
4. 在角色或系统托盘图标上右键，展开「切换人物」，随时换成早坂爱或藤原。

单文件版已经把角色、面板、光标和图标全部打包进 EXE，**不用安装，也不需要额外的 `assets` 文件夹**。

> [!NOTE]
> 如果 Windows 提示「已保护你的电脑」，点击「更多信息」→「仍要运行」即可。这是未签名 EXE 的常规提示。

## 三位角色

在角色或托盘图标上右键，展开「切换人物」，直接选择目标人物（✓ 标记当前人物）；角色选择会自动保存，下次启动恢复最后选择的人物。

| 角色 | 她的样子 |
| --- | --- |
| **四宫辉夜** | 温柔又有点别扭的学生会长。摸头会害羞，喝茶时安静地陪着你。 |
| **早坂爱** | 可靠、冷静、偶尔吐槽的私人女仆。被摸头会先愣一下，再放松下来整理马尾。 |
| **藤原** | 元气满满的书记。摸头、喝茶、睡醒都活力十足。 |

心情、元气和休息状态按人物独立保存；大小、置顶、面板开关和喝水提醒为全局设置。旧版存档的状态归入当时选中的人物，其他人物从默认状态开始。

<details>
<summary><strong>看看辉夜的动作</strong></summary>

<p align="center">
  <img src="docs/states.png" width="880" alt="辉夜的状态：待机、摸摸头、一起喝茶、休息中、喝水提醒（茶会面板展开）">
</p>

</details>

<details>
<summary><strong>看看早坂爱的动作</strong></summary>

<p align="center">
  <img src="docs/hayasaka.png" width="880" alt="早坂爱的状态：待机、摸摸头时先意外再放松、一起喝茶、休息中、喝水提醒">
</p>

</details>

<details>
<summary><strong>看看藤原的动作</strong></summary>

<p align="center">
  <img src="docs/fujiwara.png" width="880" alt="藤原的状态：待机、摸摸头、一起喝茶、休息中、喝水提醒">
</p>

</details>

## 亮点

- **原生实现，轻量安静**：C# + WinForms，不依赖 Electron、Node.js 或浏览器内核。
- **一个 EXE，开箱即用**：下载、双击，就这么简单。
- **三位角色，随时切换**：辉夜、早坂爱和藤原各有自己的状态、姿势和台词。
- **高清不糊**：按 Windows 显示缩放（100%–300%）匹配对应分辨率的面板素材。
- **会互动的桌宠**：摸头、喝茶、休息、睡觉都有不同反应；早坂爱被摸头时还有两段式的小反应。
- **完整的茶会面板**：状态、动作、置顶、大小、隐藏，一应俱全。
- **不打扰你**：不占任务栏，可始终置顶，也可隐藏后从系统托盘找回。
- **完全离线**：不联网、不收集数据，设置只保存在本机。
- **记住你的习惯**：人物、置顶、面板开关等选择会在下次启动时恢复。

## 怎么玩

| 操作 | 效果 |
| --- | --- |
| 单击角色 | 摸摸头，心情 +8 |
| 按住并拖动 | 把她搬到桌面上的任意位置 |
| 右键角色 | 打开菜单：喝茶、休息、置顶、切换人物、喝水提醒、隐藏、退出…… |
| 点击底部状态栏 | 展开 / 收起茶会面板 |
| 切换人物 | 在菜单中展开「切换人物」，选择辉夜、早坂爱或藤原 |
| 一起喝茶 | 元气 +20，心情 +5 |
| 摸摸头 | 心情 +8 |
| 休息一会 | 进入 / 结束休息状态 |
| 角色大小滑块 | 在 50%～120% 之间按 1% 调整，拖动实时预览，松手保存 |
| 喝水提醒 | 右键点击系统托盘里的角色图标，在菜单中开关“喝水提醒”；直接在菜单的“提醒间隔”输入框修改 1～1440 分钟，回车或点击别处保存有效值，Esc 取消修改 |
| 暂时隐藏 | 从桌面隐藏，可从系统托盘找回 |

首次运行默认出现辉夜，角色大小默认为 70%，喝水间隔默认为 30 分钟。修改后的大小和喝水设置会自动保存，下次启动继续使用。

元气每 5 分钟减少 1；清醒时心情每 5 分钟减少 1，休息时每 5 分钟增加 1。两项数值都限制在 0–100。休息时不能喝茶或摸头。

早坂爱被摸头时会先露出约 0.6 秒略感意外的表情，再放松微笑并整理马尾，约 3.2 秒后回到日常。她不会使用辉夜的害羞摇晃，心情奖励与睡眠限制与其他角色一致。

喝水提醒默认关闭、间隔 30 分钟。开启后，当前角色到时间会提醒你喝水并直接递出水杯；设置会自动保存。休息时照常提醒；暂时隐藏时会在到期后重新出现并提醒。拖动、编辑间隔、打开菜单或已有对话时短暂等待，之后只提醒一次。重新启动或修改间隔后重新计时。

<details>
<summary><strong>看看茶会面板</strong></summary>

<p align="center">
  <img src="docs/panel.png" width="680" alt="茶会面板（当前人物为辉夜）">
</p>

面板标题和「叫醒」等文字会随当前人物变化。

</details>

<details>
<summary><strong>看看右键菜单</strong></summary>

<p align="center">
  <img src="docs/context-menu.png" width="210" alt="右键菜单，含切换人物子菜单">
</p>

</details>

## 数据与卸载

- **存档位置**：`%APPDATA%\kaguya-native-pet\pet-state.json`
- **清空状态**：退出程序后删除 `%APPDATA%\kaguya-native-pet` 文件夹。
- **卸载**：删除 `KaguyaPet.exe` 即可，程序不会安装服务或写入系统目录。

## 常见问题

**需要安装 .NET 吗？**  
Windows 10 / 11 已自带 .NET Framework 4.x，通常无需额外安装。

**需要管理员权限吗？**  
不需要，普通用户权限即可运行。

**角色不见了怎么办？**  
点击系统托盘里的角色图标，从托盘菜单中找回。

**为什么提示「未知发布者」？**  
EXE 没有做代码签名。源码全部公开，可以自行检查或构建。

**能开机自启吗？**  
目前没有内置开关；可以把 `KaguyaPet.exe` 的快捷方式放进 `shell:startup` 启动文件夹。

## 从源码构建

<details>
<summary><strong>开发者入口</strong></summary>

需要 Windows 自带的 .NET Framework 编译器，无需 Node.js 或 Electron。

```bat
build.cmd
RunKaguya.cmd
```

- `build.cmd`：编译生成 `KaguyaPet.exe`
- `RunKaguya.cmd`：启动（此版本需要与 `assets` 文件夹放在一起）
- 单文件版：`powershell -NoProfile -ExecutionPolicy Bypass -File tools\build-single-file.ps1`

更详细的构建、DPI 素材导出和验证说明见 [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)。

</details>

## 角色素材

三位角色的姿势素材都在本地完成去底、裁切和脚底对齐。早坂爱和藤原的姿势由参考图经 image2 生成，生成源图与提示词只保留在本地 `output/` 目录，不随仓库发布。

- **辉夜**：日常、开心、喝茶、睡眠、递水和喝水六套姿势，以及面板素材。
- **早坂爱**：`tools/prepare-hayasaka-sprites.py` 处理日常、开心、喝茶、睡眠、递水和喝水姿势；`tools/prepare-hayasaka-headpat.py` 将摸头动画的两帧处理为 `assets/hayasaka/pat-notice.png` 和 `pat-relaxed.png`。
- **藤原**：`tools/prepare-fujiwara-sprites.py` 从 3×2 姿势表生成 `assets/fujiwara/` 下的六个姿势。
- 单文件发布版已嵌入三位角色的素材，无需携带 `assets` 文件夹。

早坂爱的台词是基于人设的原创改写，不是原作名言，表现私下放松、简短吐槽、有边界但愿意接受熟人关心的一面。

参考资料：
- 动画官方人物介绍：https://kaguya.love/1st/character/ （场合身份切换、与辉夜如姐妹的关系）
- ABEMA 角色资料：https://times.abema.tv/articles/-/10016603 （冷静、工作压力、私下兴趣与放松；媒体转引设定集）

## 致谢

抓手指针资源来自 [Chromium](https://github.com/chromium/chromium)，许可证见 [`assets/cursors/CHROMIUM-LICENSE.txt`](assets/cursors/CHROMIUM-LICENSE.txt)。

## 转载说明

本项目（包括 README、角色图片、面板素材和打包后的程序）仅允许分享本仓库或 [Releases](../../releases/latest) 页面的原链接；请勿复制、镜像、搬运或二次发布到其他平台。如有合作或其他用途，请先联系作者。

---

<p align="center"><sub>如果她们让你的桌面多了一点温度，欢迎点个 ⭐ Star。</sub></p>
