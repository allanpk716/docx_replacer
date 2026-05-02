# 发布流程指南

本文档记录 DocuFiller 从代码到 GitHub Release 和内网更新服务器的完整发布流程。

## 发布前置检查

1. 确认所有待发布的代码已合并到 `master` 分支
2. 确认 `dotnet build` 和 `dotnet test` 通过
3. 清理残留的测试 tag：`git tag -l '*test*'`，如有则删除

## GitHub Release 发布

### 步骤

```bash
# 1. 切到 master 并拉取最新
git checkout master && git pull

# 2. 打 tag 并推送（Actions 自动触发构建和发布）
git tag -a v<版本号> -m "v<版本号> - <简要描述>"
git push origin v<版本号>
```

### 等待构建

```bash
# 查看 Actions 运行状态
gh run list --repo allanpk716/docx_replacer --limit 1
```

构建通常需要 2-3 分钟。

### 验证 Release 产物

构建完成后，确认 Release 包含全部 4 个文件：

```bash
gh release view v<版本号> --repo allanpk716/docx_replacer --json assets --jq '.assets[].name'
```

期望输出：

```
DocuFiller-win-Setup.exe          # 安装包，用户下载这个安装
DocuFiller-win-Portable.zip       # 便携版，解压即用（不支持自动更新）
DocuFiller-<版本号>-full.nupkg     # Velopack 更新包（用户不需要关心）
releases.win.json                  # Velopack 版本清单
```

如果缺少文件，**不要删 tag 重建**，直接手动上传到已有 Release：

```bash
# 手动上传缺失文件到已有 Release
gh release upload v<版本号> <文件路径> --repo allanpk716/docx_replacer
```

## 内网更新服务器上传

> **凭据来源**：所有敏感信息（服务器地址、端口、API Token）从项目根目录的 `.env` 文件读取。
> `.env` 已在 `.gitignore` 中排除，不会被提交。
> 如果 `.env` 不存在或缺少字段，发布前会报错提示配置。
>
> `.env` 所需字段：
>
> | 字段 | 说明 |
> |------|------|
> | `UPDATE_SERVER_HOST` | 服务器 IP 或主机名 |
> | `UPDATE_SERVER_PORT` | HTTP 端口 |
> | `UPDATE_SERVER_API_TOKEN` | Bearer Token |
> | `UPDATE_SERVER_SSH_PORT` | SSH 端口（仅 SSH 部署用） |
> | `UPDATE_SERVER_USER` | SSH 用户名（仅 SSH 部署用） |
> | `UPDATE_SERVER_PASSWORD` | SSH 密码（仅 SSH 部署用） |

### 下载产物

```bash
mkdir -p /tmp/v<版本号> && cd /tmp/v<版本号>
gh release download v<版本号> --repo allanpk716/docx_replacer --clobber
```

### 上传到 stable 通道

```bash
# 从 .env 加载环境变量（bash）
set -a; source .env; set +a

# 上传 .nupkg（更新包）
curl -X POST "http://${UPDATE_SERVER_HOST}:${UPDATE_SERVER_PORT}/api/channels/stable/releases" \
  -H "Authorization: Bearer ${UPDATE_SERVER_API_TOKEN}" \
  -F "file=@DocuFiller-<版本号>-full.nupkg"

# 上传 releases.win.json（版本清单，multipart field name 必须是 file）
curl -X POST "http://${UPDATE_SERVER_HOST}:${UPDATE_SERVER_PORT}/api/channels/stable/releases" \
  -H "Authorization: Bearer ${UPDATE_SERVER_API_TOKEN}" \
  -F "file=@releases.win.json"
```

### 上传到 beta 通道

将上面 URL 中的 `stable` 替换为 `beta` 即可。

### 验证

```bash
curl -s "http://${UPDATE_SERVER_HOST}:${UPDATE_SERVER_PORT}/api/channels/stable/releases"
```

确认新版本出现在版本列表中。

### 清理临时文件

```bash
rm -rf /tmp/v<版本号>
```

## 历史问题备忘

### Setup.exe 文件名不匹配

`vpk pack` 生成的安装程序文件名是 `DocuFiller-win-Setup.exe`（带 `-win-`），不是 `DocuFillerSetup.exe`。

workflow 中的 glob 已修复为 `artifacts/*Setup*.exe`（宽松匹配），无需担心。

### milestone merge 后 stash pop 警告

每次 milestone 合并后会出现 "already exists, no checkout" 警告，**无实际影响**，merge 本身是成功的。

已提 issue：[gsd-build/gsd-2#4766](https://github.com/gsd-build/gsd-2/issues/4766)

### releases.win.json 上传的 field name

上传 releases.win.json 时，multipart field name 必须是 `file`（不是 `filename`），否则服务器不会触发 feed 合并逻辑。
