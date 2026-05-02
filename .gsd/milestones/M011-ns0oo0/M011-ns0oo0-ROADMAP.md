# M011-ns0oo0: 更新体验修复

**Vision:** 修复更新功能的两个体验问题：(1) 更新设置窗口不回显 appsettings.json 中已配置的 UpdateUrl；(2) 下载更新时无进度反馈。让用户能确认更新源配置正确，并在下载时看到实时进度、速度和预估时间。

## Success Criteria

- 更新设置窗口正确显示 appsettings.json 中的 UpdateUrl 原始值和 Channel
- 下载更新时弹出模态进度窗口，实时显示进度条（0-100%）、下载速度（MB/s）、预估剩余时间
- 进度窗口的取消按钮能中断下载，应用继续正常运行
- 现有更新检查、状态栏提示、设置保存功能不受影响

## Slices

- [x] **S01: S01** `risk:low` `depends:[]`
  > After this: 打开更新设置窗口，URL 输入框正确显示 appsettings.json 中的 UpdateUrl（如 http://172.18.200.47:30001），Channel 下拉框显示当前通道（如 stable）

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 确认下载更新后弹出独立模态进度窗口，显示进度条（0-100%）、下载速度（MB/s）、预估剩余时间，点击取消可中断下载

## Boundary Map

Not provided.
