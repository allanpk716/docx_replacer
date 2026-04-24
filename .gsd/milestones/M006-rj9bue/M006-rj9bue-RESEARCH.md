# M006-rj9bue: 真实数据端到端回归测试 — 研究报告

## 测试数据分析

### Excel 文件

| 文件 | 格式 | 行数 | 关键词数 | 富文本 |
|------|------|------|---------|--------|
| LD68 IVDR.xlsx | 三列（ID\|关键词\|值） | 74 | 73（去重后） | 3 个单元格含上标（superscript） |
| FD68 IVDR.xlsx | 两列（关键词\|值） | 59 | 58（去重后） | 无富文本 |

### 关键词重叠分析

- 共同关键词：43 个（如 #产品名称#、#产品型号#、#Basic UDI-DI# 等）
- LD68 独有：30 个（主要是 #性能指标1上限#~#性能指标8上限# 等带"上限"/"下限"后缀的、以及 #pH#、#透明浓度#、#对比产品# 系列）
- FD68 独有：15 个（主要是 #准确度结果#、#空白批内精密度结果# 等带"结果"后缀的，以及 #临床指示1#~#临床指示7#）

### 关键词已知值（用于断言）

**LD68 (三列):**
- `#产品名称#` → `Lyse`
- `#产品型号#` → `BH-LD68`
- `#Basic UDI-DI#` → `69357407IBHS000018EF`
- `#包装规格#` → `500mL,1L,2L`
- `#产品代码#` → `USBH00000899`

**FD68 (两列):**
- `#产品名称#` → `Fluorescent Dye`
- `#产品型号#` → `BH-FD68`
- `#Basic UDI-DI#` → `69357407IBHS000017ED`
- `#包装规格#` → `12mL,25mL,25mL×2`
- `#产品代码#` → `USBC00000035`

### 富文本详情（LD68 仅）

3 个单元格含 superscript（上标）：
1. `Absorption peak wavelength: 634±10` — `10` 部分可能有上标
2. Appearance 章节内容 — 含上标
3. `WBC count ≥ 0.2×10^9/L, HGB ≥ 1g/L` — `9` 为上标

## Word 模板分析

### 目录结构

模板目录：`test_data/2026年4月23日/血细胞分析用染色液（BH-LD68）-2025年12月30日095206_2026年1月29日093518/`

### 模板分布（43 个 docx）

| 按控件数排列 | 文件 | 控件数 | 有表格 | 有页眉 | 有页脚 |
|-------------|------|--------|--------|--------|--------|
| **推荐测试** | CE01 Device Description | 82 | Y | Y | Y |
| **推荐测试** | CE04 General Safety | 54 | Y | Y | Y |
| **推荐测试** | CE06-01 Performance Eval Plan | 49 | Y | Y | Y |
| 高覆盖 | CE06-02 Performance Eval Report | 38 | Y | Y | Y |
| 高覆盖 | CE00 Overview | 35 | Y | Y | Y |
| 高覆盖 | CE05-02 Risk Mgmt Report | 37 | Y | Y | Y |
| 中等 | CE03 Design & Manufacturing | 18 | Y | Y | Y |
| 中等 | CE05 Benefit-Risk Analysis | 18 | Y | Y | Y |
| 中等 | CE05-01 Risk Mgmt Plan | 18 | Y | Y | Y |
| 中等 | CE05-03 Risk Analysis Tables | 16 | Y | Y | Y |
| ... | (其余 16 个有控件模板, 3-14 控件) | | | | |
| 无控件 | 17 个表单/流程图模板 | 0 | | | |

### 推荐测试模板选择

1. **CE01**（Chapter 1, 82 控件, 表格+页眉+页脚）— 最全面，替换正确性主测试
2. **CE06-01**（Chapter 6, 49 控件, 表格+页眉+页脚）— 不同 Chapter，结构不同
3. **CE00**（Chapter 0, 35 控件, 表格+页眉+页脚）— Overview 类型，控件分布不同

## 版本兼容性关键信息

### DocumentProcessorService 构造函数

**当前代码（M004 后）** — 8 个参数：
1. `ILogger<DocumentProcessorService>`
2. `IExcelDataParser`
3. `IFileService`
4. `IProgressReporter`
5. `ContentControlProcessor`
6. `CommentManager`
7. `IServiceProvider`
8. `ISafeFormattedContentReplacer`

**d81cd00（M004 前）** — 9 个参数，多了 `IDataParser`（位置不确定，需反射检测）

### ServiceFactory 策略

使用 `ServiceCollection` DI 自动解析：
- 注册所有已知服务接口和实现
- 条件检测 `IDataParser` 接口是否存在（通过 `AppDomain.CurrentDomain.GetAssemblies()`）
- 如果存在，查找 `DataParserService` 实现并注册
- DI 容器自动匹配构造函数参数，无需手动指定参数顺序

### csproj 条件源文件链接

```xml
<Compile Include="..\Services\DataParserService.cs" Condition="Exists('..\Services\DataParserService.cs')" />
<Compile Include="..\Services\Interfaces\IDataParser.cs" Condition="Exists('..\Services\Interfaces\IDataParser.cs')" />
```

在当前代码上（文件不存在）：条件不满足，跳过编译。
在 d81cd00 上（文件存在）：条件满足，正常编译。

## 测试数据路径发现

test_data/ 在 .gitignore 中被排除，不在 worktree 中存在。
路径：`C:/WorkSpace/agent/docx_replacer/test_data/2026年4月23日/`

发现策略：从测试程序集位置向上导航，查找包含 `test_data` 子目录的目录。

## 注意事项

1. 两个 Excel 文件都在同一目录下，TestDataHelper 需要暴露两个路径属性
2. 模板目录以 LD68 命名，但关键词与 FD68 高度重叠（43 个共同），可用于两种格式测试
3. FD68 独有的关键词可能在模板中无对应控件——测试时只需验证共同关键词匹配即可
4. CE01 是最佳测试模板（82 控件最多，覆盖面最广）
5. 所有有控件的模板都同时有页眉和页脚控件
6. LD68 有 3 个上标富文本单元格，FD68 没有——这自然提供了"有富文本"和"无富文本"的对比测试场景
