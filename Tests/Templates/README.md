# 测试模板创建说明

本目录用于存放测试用的 Word 模板文件。

## 需要创建的测试模板

### 1. template-with-header.docx
包含页眉内容控件的测试模板。

**创建步骤**:
1. 打开 Microsoft Word
2. 插入 > 页眉 > 空白
3. 在页眉中插入内容控件：开发工具 > 控件 > 纯文本内容控件
4. 设置控件属性：开发工具 > 属性 > 标记：`HeaderField1`
5. 保存为 `template-with-header.docx`

### 2. template-with-footer.docx
包含页脚内容控件的测试模板。

**创建步骤**:
1. 打开 Microsoft Word
2. 插入 > 页脚 > 空白
3. 在页脚中插入内容控件
4. 设置控件标记：`FooterField1`
5. 保存为 `template-with-footer.docx`

### 3. template-with-both.docx
同时包含页眉、页脚和正文内容控件的测试模板。

**创建步骤**:
1. 打开 Microsoft Word
2. 插入页眉并添加控件 `HeaderField1`
3. 插入页脚并添加控件 `FooterField1`
4. 在正文添加控件 `BodyField1`
5. 保存为 `template-with-both.docx`

### 4. template-odd-even.docx
奇偶页不同页眉的测试模板。

**创建步骤**:
1. 打开 Microsoft Word
2. 页面布局 > 小三角 > 勾选"奇偶页不同"
3. 在奇数页页眉添加控件 `HeaderField1`
4. 在偶数页页眉添加控件 `HeaderField2`
5. 保存为 `template-odd-even.docx`

## 控件标记说明

- `HeaderField1`: 页眉中的测试字段
- `FooterField1`: 页脚中的测试字段
- `BodyField1`: 正文中的测试字段
- `HeaderField2`: 偶数页页眉中的测试字段

## 测试数据

对应的测试数据文件位于 `../Data/test-data.json`。
