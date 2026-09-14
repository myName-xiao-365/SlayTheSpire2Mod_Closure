# 可露希尔 ClosureMod

这是一个《Slay the Spire 2》的可露希尔角色 Mod。她围绕费用、债务、迟钝、无人机模块等机制展开，目前仍在开发中。

## 下载

推荐玩家从 GitHub 的 **Releases** 页面下载最新版本压缩包：

<https://github.com/myName-xiao-365/SlayTheSpire2Mod_Closure/releases>

下载名为 `ClosureMod.zip` 或类似名称的压缩包即可。不要下载 `Source code`，那个是给开发者看的源码包，不能直接当作 Mod 使用。

## 安装

1. 安装并启用《Slay the Spire 2》的 Mod 加载环境。
2. 安装依赖 Mod：`STS2-RitsuLib`。
3. 打开游戏目录下的 `mods` 文件夹。Steam 默认路径通常类似：

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

4. 将下载的 `ClosureMod.zip` 解压到 `mods` 目录中，最终目录应类似：

```text
Slay the Spire 2\mods\ClosureMod\ClosureMod.json
Slay the Spire 2\mods\ClosureMod\ClosureMod.dll
Slay the Spire 2\mods\ClosureMod\ClosureMod.pck
```

5. 启动游戏，在 Mod 列表中确认 `可露希尔` 已启用。

## 常见问题

如果游戏里看不到角色，请先确认：

- `STS2-RitsuLib` 已安装并启用。
- `ClosureMod` 文件夹没有多套一层目录，例如不要变成 `mods\ClosureMod\ClosureMod\ClosureMod.json`。
- 游戏版本满足 Mod 要求。当前 `ClosureMod.json` 要求最低游戏版本为 `0.106.0`。

如果只有代码文件、没有 `ClosureMod.dll` 或 `ClosureMod.pck`，说明你下载的是源码，不是可直接游玩的发布包。

## 开发者

源码工程位于：

```text
src/ClosureMod
```

编译前请准备 .NET SDK、Godot/MegaDot 导出环境，以及 `STS2-RitsuLib` 依赖。完整构建命令：

```powershell
dotnet build src/ClosureMod/ClosureMod.csproj -c Release
```

如果只想检查 C# 代码，不导出 pck，可以使用：

```powershell
dotnet build src/ClosureMod/ClosureMod.csproj -c Release /p:CopyModOnBuild=false /p:RunPckExport=false
```

本仓库只保留正式源码和已经导入 Mod 的资源。原始卡面、临时导出、工具目录、Godot 导入缓存和本地游戏路径配置不会提交。
