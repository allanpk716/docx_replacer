---
phase: complete
phase_name: Milestone Completion
project: DocuFiller
generated: "2026-05-02T16:21:11Z"
counts:
  decisions: 2
  lessons: 3
  patterns: 1
  surprises: 0
missing_artifacts: []
---

### Decisions

- 去掉 ExplicitChannel 而非设置 ExplicitChannel='win'：选择完全不设置 ExplicitChannel，让 Velopack 按 OS 默认 channel 工作，避免未来 OS 移植时再次遗漏。更简洁，行为更可预测。 `Source: S01-SUMMARY.md/核心修改`
- GitHub 模式跳过 beta→stable 回退：GitHub Releases 只分发 stable 版本（D028），回退检查无意义，直接跳过减少不必要的网络请求。 `Source: S01-SUMMARY.md/核心修改`

### Lessons

- Velopack ExplicitChannel 覆盖 OS 默认 channel 名称（Windows='win'），设置不当会导致查找错误的 releases 文件（如 releases.stable.json 而非 releases.win.json），导致更新检测完全失败。根本原因是 M010 添加通道功能时设置了 ExplicitChannel="stable" 与 Velopack 的 channel 文件命名约定冲突。 `Source: S01-SUMMARY.md/变更概述`
- Velopack UpdateOptions 默认行为（不设置 ExplicitChannel）通常是正确选择，除非有明确的跨 OS channel 统一需求。设置 ExplicitChannel 会覆盖 Velopack 的自动 channel 检测，容易引入 bug。 `Source: S01-SUMMARY.md/验证结果`
- 内网 HTTP 模式和 GitHub 模式的回退逻辑必须分开处理：HTTP 需要用 _baseUrl + targetChannel 创建新 SimpleWebSource，GitHub 模式只走 stable 通道所以回退检查无意义。混用逻辑会导致 GitHub 模式产生无效的网络请求。 `Source: M014-jkpyu6-VALIDATION.md/Success Criteria Checklist`

### Patterns

- 回退逻辑需要显式守卫区分更新源类型：使用 `if (_sourceType == "HTTP")` 守卫确保回退逻辑只在适用的源类型上执行，避免跨模式产生无效操作。 `Source: S01-SUMMARY.md/核心修改`

### Surprises

(无意外发现)
