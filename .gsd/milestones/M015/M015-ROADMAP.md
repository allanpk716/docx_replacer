# M015: GitHub 更新源从 API 切换到 CDN 直连

**Vision:** 将 GitHub 更新模式从 GithubSource（GitHub API，60 次/小时 rate limit）切换为 SimpleWebSource（CDN 直连 /releases/latest/download/，无 rate limit），统一内网和 GitHub 两种源的底层实现为 SimpleWebSource，消除匿名 API 调用的频率限制问题。内网 HTTP 更新逻辑不受影响。

## Success Criteria

- GitHub 模式使用 SimpleWebSource 直接下载 release 资产，不调用 GitHub API
- 内网 HTTP 模式更新逻辑完全不受影响，行为不变
- dotnet build 无错误，dotnet test 全部通过
- 代码中不再引用 GithubSource 类

## Slices

- [x] **S01: S01** `risk:low` `depends:[]`
  > After this: GitHub 更新模式不再受 API rate limit 限制，匿名用户可无限次检查更新

## Boundary Map

Not provided.
