---
id: M013-ueix00
title: "更新配置持久化路径修复"
status: complete
completed_at: 2026-05-02T13:17:33.713Z
key_decisions:
  - GetPersistentConfigPath() 改为 public static 无条件返回 ~/.docx_replacer/update-config.json，彻底消除 Velopack 安装目录依赖
  - UpdateSettingsViewModel 复用 UpdateService.GetPersistentConfigPath() 而非复制路径逻辑
  - 新增 internal 构造函数用于测试注入临时路径
  - 不做旧路径配置迁移（用户确认）
key_files:
  - Services/UpdateService.cs
  - ViewModels/UpdateSettingsViewModel.cs
  - Tests/UpdateServiceTests.cs
lessons_learned:
  - 配置文件路径不能放在应用安装目录下，任何安装/更新机制都可能覆盖
  - Operational/UAT 验证类在有结构性保证时可以用结构证明替代运行时测试，但必须在验证文档中明确说明理由
---

# M013-ueix00: 更新配置持久化路径修复

**将 update-config.json 从 Velopack 安装目录迁移到 ~/.docx_replacer/，彻底隔离安装/更新生命周期，新增 8 个测试覆盖路径逻辑和边界场景**

## What Happened

S01 将 GetPersistentConfigPath() 从基于 Velopack 安装结构的有条件检测改为无条件返回 %USERPROFILE%\.docx_replacer\update-config.json。新增 Directory.CreateDirectory() 自动创建目录，新增 internal 构造函数用于测试路径注入。UpdateSettingsViewModel.ReadPersistentConfig() 重构为调用共享的 GetPersistentConfigPath()。S02 新增 5 个边界测试（malformed JSON、缺失字段、空文件、文件覆盖保护），全量 249 测试通过。验证阶段修复了 Operational/UAT 验证类的文档问题，明确说明结构证明的理由。

## Success Criteria Results



## Definition of Done Results



## Requirement Outcomes



## Deviations

None.

## Follow-ups

None.
