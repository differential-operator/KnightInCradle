# 构建与验证

## 本地依赖

- Windows、.NET SDK，以及 .NET Framework 4.7.2 目标框架支持。
- 已安装 BepInEx 5.x 的 AliceInCradle 0.30i。
- 测试项目使用 net10.0 或 net472；运行对应测试时需要支持该目标的 SDK/运行时。

游戏目录应含 `AliceInCradle_Data/Managed/Assembly-CSharp.dll` 和 `BepInEx/core/`。
主工程通过 `AicRoot` 引用目标游戏；不能直接使用另一游戏版本的程序集。

## 准备资源

恢复本地开发资源到以下位置（均已被 Git 忽略）：

- `audio_embed/`：工程文件逐项列出的 WAV 嵌入资源，编译时必需。
- `assets/hk/`：角色、技能、装饰与音效等运行素材。
- `charm_ui/`：护符界面 `layout.json` 和 `images/`。

这些资源从已有的本地开发目录恢复；完整安装包提供运行素材，但不包含独立的编译音频目录。
不能只克隆源码就获得完整可运行安装包。

## 编译

```powershell
$env:AIC_GAME_ROOT = 'D:\Games\AliceInCradle_ver030'
dotnet build -c Release
# 也可直接传入 -p:AicRoot="<游戏目录>"
```

输出：`bin/Release/net472/KnightInCradle.dll`。

## 部署

先正常退出游戏，并恢复上述资源目录，然后执行：

```powershell
./build_and_deploy.ps1 -GameRoot $env:AIC_GAME_ROOT
# 只部署已编译 DLL：
./deploy.ps1 -Configuration Release -GameRoot $env:AIC_GAME_ROOT
```

缺少运行资源时脚本停止，不进行部分部署。目标 `plugins` 中只应有一份模组 DLL。

## 回归检查

不需要启动游戏的规则测试：

```powershell
$tests = 'RespawnLifecycleTest','RespawnArrivalTest','CharmEquipmentTest','DangerDebugTest','DangerHealthTest','EventSessionTest','JumperContactTest','LeafFlutterTest','OverlayEscapeTest','RoomTransferTest','TerrainCollisionTest'
foreach ($name in $tests) {
    dotnet run --project "tools/$name" -c Release
    if ($LASTEXITCODE -ne 0) { throw "$name failed" }
}
```

原生类型检查需先编译模组，并向可执行文件传入仓库路径和游戏路径：

```powershell
dotnet build tools/MapRevealTest -c Release
& ./tools/MapRevealTest/bin/Release/net472/MapRevealTest.exe (Get-Location).Path $env:AIC_GAME_ROOT
dotnet build tools/CrumbleFloorTest -c Release
& ./tools/CrumbleFloorTest/bin/Release/net472/CrumbleFloorTest.exe (Get-Location).Path $env:AIC_GAME_ROOT
```

这些检查不能替代游戏内的碰撞、过图、剧情和装置实测。`FxSyncTest` 是附加同步回归工具，需要本地 BepInEx 引用。
