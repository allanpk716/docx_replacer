# M017 Discussion Log

## Exchange — 2026-05-03T04:08:13.796Z

### Depth Check

确认一下我的理解：模板 TextBox 和数据 TextBox 的文件拖放被 WPF TextBox 内置处理拦截（显示"禁止"图标），清理区域 Border 正常。修复方案是把冒泡事件改为 Preview 隧道事件。这个理解正确吗？

- **Yes, you got it (Recommended)** — 理解正确，直接开始写里程碑文件
- **Not quite — let me clarify** — 有遗漏或理解偏差，我补充说明

**Selected:** Yes, you got it (Recommended)

---
