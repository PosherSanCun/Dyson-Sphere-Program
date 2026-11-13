# 戴森球计划：联机存档与蓝图

此仓库直接位于游戏的数据目录。同步 `Blueprint/`、`Blueprints/` 下的 `.txt` 蓝图、`Save/` 下有名称的手动存档，以及 `Mods/QuickLogisticsStore/` 下的自制收纳模组。

`.dsv` 与同名 `.server` 必须一起同步：后者是 Nebula 联机数据。两种文件均通过 Git LFS 保存，不能像文本一样合并。`_autosave_*`、`_lastexit_`、`options.xml`、`player.key`、`Achievement/`、`Property/` 和 Steam Cloud 文件不入库，原文件保留在本机。

## 安装机甲物流一键收纳模组

先保存并完全退出游戏，等待 Steam Cloud 同步结束后再拉取仓库，按 [模组安装说明](./Mods/QuickLogisticsStore/README.md) 安装 **Quick Logistics Store 1.0.1**。仓库中的 `Mods/` 是分发目录；游戏需要从自己的 `BepInEx/plugins/` 目录加载 DLL，单纯拉取不会自动安装。

退出游戏后，在仓库目录运行以下命令，把示例路径改为自己的游戏安装目录（其中应有 `DSPGAME.exe` 和已安装的 BepInEx）：

```powershell
.\Mods\QuickLogisticsStore\Install.ps1 -GameDirectory 'D:\SteamLibrary\steamapps\common\Dyson Sphere Program'
```

重启游戏，打开背包，点击「物流」标题后的小收纳按钮，或按 **左 Ctrl + 左 Shift + L**。它只把普通背包物品补入已经设置物品的机甲物流格。

## 必须使用正确的本地目录

克隆或拉取后，必须确认仓库根目录就是游戏实际使用的数据目录，而不是其 `Save` 子目录，也不是 `Documents\GitHub` 下的独立副本。默认位置为：

```text
C:\Users\<你的用户名>\Documents\Dyson Sphere Program\
├── .git\
├── .gitignore
├── .gitattributes
├── README.md
├── Save\           ← 游戏读取和写入的 .dsv、同名 .server
├── Blueprint\
└── Blueprints\
```

如果系统重定向了“文档”目录，请以游戏实际的数据目录为准。不要使用 `Save\Dyson-Sphere-Program\Save` 这样的嵌套结构。只复制存档到游戏目录、却把 Git 仓库留在其他位置，会导致游戏后续保存无法出现在该仓库的 Git 变更中。

### 已经克隆到其他位置时

1. 退出游戏，等待 Steam Cloud 同步结束，备份游戏数据目录和已克隆的仓库。
2. 确认游戏数据目录没有另一个 `.git` 仓库；如果已有，先核对仓库归属，不要覆盖。
3. 将克隆仓库的 `.git`（隐藏目录）、`.gitignore`、`.gitattributes`、`README.md` 放到游戏数据目录根层；将仓库的 `Save`、`Blueprint`、`Blueprints` 内容合并到游戏目录中的同名文件夹。
4. 同名存档或蓝图内容不一致时先保留双方副本，确认要使用的版本再处理；联机存档的 `.dsv` 与同名 `.server` 成对处理。保留本机个人配置、身份、自动存档和 Steam Cloud 文件。
5. 将原位置的剩余副本移到游戏目录之外作为备份，避免嵌套副本被 Git 重复收录。在 Git 客户端中重新添加游戏数据目录作为本地仓库。
6. 在游戏数据目录运行 `git rev-parse --show-toplevel` 和 `git status --short`，确认仓库根目录正确、差异符合预期；再运行 `git lfs ls-files`，确认存档为实际文件而非未下载的指针。

调整后，游戏保存有名称的手动存档会直接显示为 Git 修改或新增文件，可在退出游戏并等待同步完成后提交、推送。自动存档 `_autosave_*` 和退出存档 `_lastexit_` 按 `.gitignore` 忽略；Git 不会自动提交或推送。日后只需在这个正确目录中拉取，不需要重复迁移。

## 约定

- 同步前退出游戏，并等待 Steam Cloud 同步结束，避免文件仍在写入。若 Steam Cloud 与 Git 反复覆盖同一存档，再调整 Steam Cloud 设置。
- 每轮联机指定一位房主负责提交共享存档。更换房主前，旧房主先上传，新房主再下载。
- 房主把进度保存到固定名称，例如 `联机主档`。自动存档不会上传。
- 每位玩家都可提交蓝图；不同方案使用不同文件名。同名蓝图也禁止自动合并，冲突时需人工选择，避免破坏蓝图编码或校验。
- 所有操作在此目录执行。不要在有本地修改时强制拉取，不要使用强制推送覆盖别人的进度。

## 本机首次准备

已初始化 `main` 分支并启用本仓库的 Git LFS。远程仓库尚需配置。

退出游戏后，检查待提交内容，再创建首次快照：

```powershell
git status --short
git add -A
git diff --cached --stat
git commit -m "初始化戴森球联机存档与蓝图"
```

确认远程服务支持 Git LFS，然后连接自己的远程仓库。以下地址是占位符，需要替换：

```powershell
git remote add origin <仓库地址>
git push -u origin main
```

## 每次开玩前

关闭游戏，在 PowerShell 中执行：

```powershell
git status --short
git pull --ff-only
git lfs pull
```

若第一条命令显示本地修改，先确认其来源。需要保留的修改先提交；若与远程进度冲突，暂停同步，由本轮房主确认要保留的版本。下载失败时不要启动共享存档；`.dsv` 不应成为只有几行文字的 LFS 指针文件。

## 房主结束联机后

手动保存共享进度，然后退出游戏：

```powershell
git status --short
git add -A
git diff --cached --stat
git commit -m "更新联机进度与蓝图"
git push
```

没有变化时无需提交。若推送被拒绝，先停止交接；不要强制推送。保存双方文件的目录外副本，再由双方确认存档版本。LFS 不会合并游戏世界的进度。

仅上传蓝图时，把 `git add -A` 改成 `git add -A -- Blueprint/ Blueprints/`，并检查暂存区没有夹带存档。

## 朋友首次接入

1. 安装 Git 与 Git LFS，并执行 `git lfs install`。Git 身份和远程仓库访问权限需要各自配置。
2. 退出游戏。若已有游戏数据目录，先在资源管理器中将整个 `Dyson Sphere Program` 文件夹改名为备份，保留其中所有内容。
3. 克隆到游戏原本的数据目录，地址需替换为实际远程仓库：

   ```powershell
   git clone <仓库地址> "$env:USERPROFILE\Documents\Dyson Sphere Program"
   cd "$env:USERPROFILE\Documents\Dyson Sphere Program"
   git lfs pull
   git config pull.ff only
   ```

4. 如“文档”目录被 OneDrive 等重定向，请使用游戏实际的数据目录。可从备份复制本机原有的 `options.xml`、`player.key`、`Achievement/`、`Property/` 到新目录，保留个人设置与身份；不要复制其他玩家的 `player.key`。
5. 要加入自己的蓝图，先复制到新目录；同名文件先比较或改名。不要直接用个人旧存档覆盖共享存档。
6. 确认 `.dsv` 已下载为完整二进制文件后再启动游戏。联机各方需自行保持游戏及联机 Mod 版本一致，此仓库不分发游戏或 Mod。

## 空间与历史

存档每次提交都可能产生新的 LFS 对象，应留意远程服务的 LFS 存储与流量额度。删除当前文件不会自动删除历史占用；不要为了缩小仓库擅自重写多人使用的历史。

参考：[Nebula 官方存档与玩家数据说明](https://github.com/NebulaModTeam/nebula/wiki/Setup-Headless-Server)、[Git LFS](https://git-lfs.com/)。
