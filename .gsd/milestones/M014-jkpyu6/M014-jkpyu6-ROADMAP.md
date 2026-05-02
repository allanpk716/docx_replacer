# M014-jkpyu6: ExplicitChannel 导致 GitHub 更新检测失败

**Vision:** 修复 Velopack ExplicitChannel=stable 导致查找 releases.stable.json 而非 releases.win.json 的 bug，去掉 ExplicitChannel，修正内网 HTTP 模式的 beta→stable 回退逻辑，确保所有更新场景正常工作。

## Success Criteria

- 安装版 v1.3.4 通过 GitHub 源能检测到 v1.4.0
- 内网 HTTP stable 通道能检测到更新
- 内网 HTTP beta→stable 回退逻辑正确创建新 SimpleWebSource
- 所有现有测试通过，无编译错误

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: 安装版 v1.3.4 启动后通过 GitHub 检测到 v1.4.0，状态栏显示有新版本可用

## Boundary Map

### 无跨 slice 依赖（单 slice 里程碑）
