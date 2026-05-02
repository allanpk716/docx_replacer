# M013-ueix00: 更新配置持久化路径修复

**Vision:** 将 update-config.json 从 Velopack 安装目录迁移到 ~/.docx_replacer/，彻底隔离安装/更新生命周期，解决配置被覆盖的反复出现的 bug。

## Success Criteria

- 配置文件路径为 %USERPROFILE%\.docx_replacer\update-config.json，Setup 安装和 Velopack 自动更新后配置不丢失
- GUI 保存配置和 CLI 读取配置使用相同路径
- dotnet build 0 errors, dotnet test 全部通过

## Slices

- [x] **S01: S01** `risk:medium` `depends:[]`
  > After this: UpdateService 和 UpdateSettingsViewModel 都从 ~/.docx_replacer/update-config.json 读写配置，dotnet build 通过

- [x] **S02: S02** `risk:low` `depends:[]`
  > After this: 所有现有测试通过，新增路径逻辑有测试覆盖

## Boundary Map

### S01 → S02

Produces:
- Services/UpdateService.cs → GetPersistentConfigPath() 返回 ~/.docx_replacer/update-config.json
- ViewModels/UpdateSettingsViewModel.cs → ReadPersistentConfig() 使用相同路径

Consumes: nothing (S01 is leaf)

### S02 consumes S01

Consumes from S01:
- UpdateService.cs → GetPersistentConfigPath() 路径逻辑
- UpdateSettingsViewModel.cs → ReadPersistentConfig() 路径逻辑
