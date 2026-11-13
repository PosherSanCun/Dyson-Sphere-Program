# 机甲物流一键收纳 1.0.1

打开机甲背包，点击「物流」标题后的托盘箭头小按钮，或按 **左 Ctrl + 左 Shift + L**，把普通背包物品补入已经设置物品的机甲物流格。放不下的留在背包中，不自动设置空白格，不处理鼠标手持物品。完成后显示收纳种类与件数。

## 参与者安装

需要已经安装 **BepInEx 5.4.17 或更高的 5.x 版本**。开发和检查使用的游戏版本为 **0.10.35.29151**。本目录不包含游戏、BepInEx、Nebula 或其他第三方 DLL。

1. 先保存并完全退出游戏，等待 Steam Cloud 同步结束后，再拉取仓库。
2. 在仓库根目录打开 PowerShell，运行下面的命令，把路径改为自己的游戏安装目录：

   ```powershell
   .\Mods\QuickLogisticsStore\Install.ps1 -GameDirectory 'D:\SteamLibrary\steamapps\common\Dyson Sphere Program'
   ```

   游戏目录应当含有 `DSPGAME.exe` 和 `BepInEx/core/BepInEx.dll`，不是存档目录。安装脚本会核对 DLL 的 SHA256；只安装本模组，旧版不同内容的 DLL 会先备份为 `.bak` 文件。

3. 重启游戏，打开背包，使用收纳按钮或快捷键。

也可以手动安装：将 [dist/QuickLogisticsStore.dll](./dist/QuickLogisticsStore.dll) 复制到自己游戏的 `BepInEx/plugins/SanCun-QuickLogisticsStore/` 下。或下载 [1.0.1 安装包](./dist/SanCun-QuickLogisticsStore-1.0.1.zip)，解压后复制其中的同名 DLL。不要安装旧的 1.0.0。

`git pull` 只更新这里的共享文件。模组发布新版后，每个人都需要在退出游戏后重新运行安装脚本，或手动更新游戏插件目录中的 DLL。安装不需要复制其他玩家的存档、身份文件或配置。

## 设置和卸载

首次启动后，快捷键配置保存在游戏自己的 `BepInEx/config/com.sancun.dsp.quicklogisticsstore.cfg`。聊天、搜索、暂停菜单中屏蔽快捷键。手动收纳不要求开启自动配送开关，也不更改物流格的物品、容量和需求设置。

卸载时退出游戏，移走 `BepInEx/plugins/SanCun-QuickLogisticsStore/QuickLogisticsStore.dll` 即可。本模组不增加存档字段。

## 源码和验证

- [src/](./src/) 为当前版本 C# 源码。使用本机游戏程序集构建，不分发游戏程序集。
- `build.ps1 -GameDirectory '<游戏目录>'` 编译并在 `dist/` 中生成 DLL、校验文件和安装包；添加 `-Install` 可同时安装。需要 Windows 自带的 .NET Framework C# 编译器。
- `tests/NativeStorageChecks.ps1 -GameRoot '<游戏目录>'` 调用真实原生容器执行 8 项容量、筛选、余量回退、重复调用及物品数量/增产点守恒检查，均已通过。
- 游戏日志曾记录成功转入 9 种、2457 件物品。1.0.1 已修复按钮 `UIButton.LateUpdate` 的初始化空引用，并通过编译和安装文件回读校验；修复版尚未完成完整界面复测及双客户端联机实测。

本模组复用游戏内部的批量转移接口；该接口不代表原版已经提供相同的用户按钮或快捷键。
