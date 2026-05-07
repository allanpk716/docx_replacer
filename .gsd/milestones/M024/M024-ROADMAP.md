# M024: 启动时更新检查进度动画

**Vision:** 消除启动时更新检查的"无反馈等待期"——从程序启动瞬间起，状态栏右下角就显示旋转动画（spinner），覆盖 5 秒延迟和实际网络检查全过程。检查完成后动画自动切换为结果状态。

## Slices

- [ ] **S01: 状态栏更新检查旋转动画** `risk:medium` `depends:[]`
  > After this: 启动程序后状态栏立刻显示旋转 spinner，检查完成后切换为结果状态

## Boundary Map

### S01 (leaf node)

Produces:
  UpdateStatusViewModel.cs → ShowCheckingAnimation 计算属性，InitializeAsync 立刻设置 Checking 状态
  MainWindow.xaml → SpinnerRotateAnimation Storyboard，spinner Canvas 元素绑定到 ShowCheckingAnimation

Consumes: nothing (唯一 slice)
