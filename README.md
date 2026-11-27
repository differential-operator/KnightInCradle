# KnightInCradle

在 AliceInCradle 中切换小骑士与诺艾尔，使用小骑士的战斗动作、技能与护符探索地图。

当前整理版本：**v26.11.2**。适配 **AliceInCradle 0.30i / BepInEx 5.x**，本地验证框架为 5.4.23.5。

## 主要功能

- 小骑士移动、跳跃、冲刺、爬墙、普攻、剑技、法术、梦钉与超级冲刺。
- 小骑士和诺艾尔使用独立的护符装备与效果。
- 小骑士初始 1 槽、最高 12 槽；诺艾尔初始 3 槽、最高 14 槽。每开启 4 个宝箱增加 1 槽。
- 兼容原版剧情、交互、过图、遭遇战、弹簧与碎裂地板。
- 小骑士 HUD 装饰、渐显；诺艾尔生命血、乔尼和蜂巢护符的血条样式。
- F10 提供键位设置、立绘显示和游戏内调试工具。

## 安装

从 [Releases](https://github.com/differential-operator/KnightInCradle/releases/latest) 下载 `KnightInCradle.zip`。将其中的 `KnightInCradle` 文件夹放到游戏的 `BepInEx/plugins`：

```text
BepInEx/plugins/KnightInCradle/
├── KnightInCradle.dll
├── assets/hk/
├── charm_ui/
└── 键位.txt
```

已有安装时合并替换；需要保留其他安装的键位时，先备份 `键位.txt`。按 F10 设置按键。

## 开发

本仓库保存源码、测试和构建脚本，不包含游戏本体、提取素材、存档或编译产物。
完整编译需要自行准备游戏引用和本地音频资源，部署还需要运行素材，见 [构建与验证](docs/BUILD.md)。

```powershell
dotnet build -c Release -p:AicRoot="D:\Games\AliceInCradle_ver030"
```

## 目录

- `src/`：模组实现。
- `tools/*Test/`：机制和兼容性回归检查。
- `docs/`：构建说明、功能变更及验证范围。
- `build_and_deploy.ps1` / `deploy.ps1`：本机编译与部署。

## 调试说明

危险度、魔物血量增幅、会心和地图全亮等选项会改变游戏状态。地图全亮不可撤销。
验证范围与待实测项见 [变更记录](docs/CHANGELOG.md)。

本项目为非官方模组。游戏及第三方素材的权利归各自权利人所有；这些素材不随源码仓库提供。
