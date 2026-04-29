---
phase: execution
phase_name: milestone-completion
project: DocuFiller
generated: "2026-04-29T17:56:00Z"
counts:
  decisions: 3
  lessons: 2
  patterns: 2
  surprises: 0
missing_artifacts: []
---

### Decisions

- **Singleton 热重载方案**: 在 IUpdateService 接口新增 ReloadSource(string updateUrl, string channel) 方法，运行时重建 IUpdateSource，保持 Singleton 生命周期不变。改为 Transient 过于激进。 Source: M010-hpylzg-CONTEXT.md/Architectural Decisions
- **独立 WPF Window 弹窗形态**: 使用独立 UpdateSettingsWindow 而非下拉面板（WPF Popup 定位问题），与 CleanupWindow 模式一致。 Source: M010-hpylzg-CONTEXT.md/Architectural Decisions
- **状态栏源类型后缀追加**: 在 UpdateStatusMessage 后追加 "(GitHub)" 或 "(内网: host)" 后缀，复用现有 TextBlock，避免新增 UI 元素。 Source: M010-hpylzg-CONTEXT.md/Architectural Decisions

### Lessons

- **Singleton 服务可通过方法级重载实现运行时行为切换**: 无需改为 Transient 或工厂模式。Remove readonly 字段修饰符 + ReloadSource 方法即可。 Source: S01-SUMMARY.md/What Happened
- **System.Text.Json.Nodes 适合配置文件局部修改**: 读 JSON → 修改特定节点 → 写回，比全序列化更安全，不会丢失其他配置节。写入失败 catch + Warning 日志，不向上抛异常。 Source: S01-SUMMARY.md/What Happened

### Patterns

- **Persist-to-config-file pattern**: 使用 System.Text.Json.Nodes 读取 JSON 配置文件，修改目标节点后写回。写入失败时 catch + Warning 日志，保证不中断业务流程。 Source: S01-SUMMARY.md/What Happened
- **状态栏信息追加模式**: 通过 getter 拼接后缀信息到现有属性（如 UpdateStatusMessage），避免在 XAML 中新增独立 TextBlock 元素。使用 OnPropertyChanged 触发刷新。 Source: S02-SUMMARY.md/What Happened

### Surprises

（无意外发现）
