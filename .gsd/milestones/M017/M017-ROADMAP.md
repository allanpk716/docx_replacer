# M017: 修复 TextBox 拖放被拦截

**Vision:** 模板文件和数据文件的 TextBox 拖放功能完全可用：用户从资源管理器拖入 .docx/.xlsx/文件夹时，TextBox 显示蓝色高亮反馈，放下后路径正确填入并触发后续操作（模板验证、数据预览）。清理区域拖放不受影响。

## Success Criteria

- 模板 TextBox 拖入 .docx 文件 → 蓝色高亮 + 路径填入 + 模板信息显示
- 模板 TextBox 拖入文件夹 → 蓝色高亮 + 文件夹处理
- 数据 TextBox 拖入 .xlsx 文件 → 蓝色高亮 + 路径填入 + 数据预览
- 拖入非匹配文件 → 错误提示
- 清理区域拖放行为不变
- dotnet build 无错误

## Slices

- [x] **S01: S01** `risk:low` `depends:[]`
  > After this: 从资源管理器拖 .docx 到模板 TextBox → 蓝色高亮 → 路径填入；拖 .xlsx 到数据 TextBox → 蓝色高亮 → 路径填入 + 数据预览

## Boundary Map

Not provided.
