# M020: 代码质量与技术债清理

**Vision:** 系统性清理项目技术债：修复静默异常吞没、虚假取消功能、大量死代码、服务间重复代码、配置幽灵值、缺失测试覆盖，以及文档不准确等问题。目标是让代码库可维护性显著提升，错误可诊断，文档与实际代码一致。

## Success Criteria

- FileService 所有方法异常被正确记录（不再裸 catch 吞掉），错误路径有测试覆盖
- 取消处理功能真正生效——CancelProcessing() 能中断正在进行的文档处理
- DocumentProcessorService 和 ContentControlProcessor 之间的 7 个重复方法提取到共享工具类
- ContentControlProcessor 中 5 个死方法、DPS 中 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp 死代码已删除
- CLAUDE.md 包含 update 子命令、完整错误码表、准确的文件结构
- FileService、TemplateCacheService 至少有基础单元测试覆盖
- appsettings.json 中未使用的配置值已清理或标注
- dotnet build 0 错误，dotnet test 全部通过

## Slices

- [ ] **S01: 关键可靠性修复：FileService 异常处理 + 取消功能** `risk:high` `depends:[]`
  > After this: FileService 所有方法捕获异常时记录日志，取消按钮真正中断处理

- [ ] **S02: 死代码清理** `risk:medium` `depends:[S01]`
  > After this: ContentControlProcessor 中 5 个死方法和 DPS 中 3 段死代码被安全移除

- [ ] **S03: 消除 DocumentProcessorService 和 ContentControlProcessor 重复代码** `risk:high` `depends:[S02]`
  > After this: 7 个重复方法提取到共享工具类 OpenXmlHelper，两个服务类引用共享实现

- [ ] **S04: CLAUDE.md 和文档准确性更新** `risk:low` `depends:[]`
  > After this: CLAUDE.md 包含 update 子命令、完整错误码表、准确文件结构和 IUpdateService 服务描述

- [ ] **S05: 配置清理和测试补充** `risk:medium` `depends:[S01]`
  > After this: appsettings.json 只保留实际使用的配置值，FileService 和 TemplateCacheService 有基础单元测试

## Boundary Map

Not provided.
