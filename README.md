# 戴森球计划：联机存档与蓝图

此仓库直接位于游戏的数据目录。同步 `Blueprint/`、`Blueprints/` 下的 `.txt` 蓝图，以及 `Save/` 下有名称的手动存档。

`.dsv` 与同名 `.server` 必须一起同步：后者是 Nebula 联机数据。两种文件均通过 Git LFS 保存，不能像文本一样合并。`_autosave_*`、`_lastexit_`、`options.xml`、`player.key`、`Achievement/`、`Property/` 和 Steam Cloud 文件不入库，原文件保留在本机。

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
