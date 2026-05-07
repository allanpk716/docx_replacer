# S01: 状态栏更新检查旋转动画 — UAT

**Milestone:** M024
**Written:** 2026-05-07T07:44:17.135Z

# S01: 状态栏更新检查旋转动画 — UAT

**Milestone:** M024
**Written:** 2026-05-07

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 这是一个纯 UI 动画特性，核心逻辑通过 ViewModel 单元测试验证，XAML 编译验证动画资源正确性。运行时视觉效果需人工确认，但功能正确性已通过自动化测试覆盖。

## Preconditions

- DocuFiller 项目已编译（dotnet build --no-restore）
- 网络可用（用于实际更新检查）

## Smoke Test

启动程序后观察状态栏右下角是否出现旋转动画，约 5-10 秒后动画是否自动停止并显示检查结果文字。

## Test Cases

### 1. 启动时立刻显示 spinner

1. 启动 DocuFiller 程序
2. **Expected:** 程序窗口出现后，状态栏右下角立刻显示旋转的虚线圆圈动画（spinner），同时"检查更新中..."文字可见

### 2. Spinner 在更新检查完成后停止

1. 启动程序后等待约 10-15 秒（覆盖 5 秒延迟 + 网络检查时间）
2. **Expected:** 旋转动画停止并消失，状态栏文字切换为更新结果（"已是最新版本" 或 "发现新版本 x.x.x"）

### 3. 手动检查更新显示 spinner

1. 程序启动后，点击状态栏中的"检查更新"链接
2. **Expected:** spinner 动画重新出现并旋转
3. 等待检查完成
4. **Expected:** 动画再次停止消失，显示检查结果

### 4. ViewModel 状态转换测试（自动化）

1. 运行 `dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation"`
2. **Expected:** 5 个测试全部通过，覆盖：
   - Default 状态下 ShowCheckingAnimation = false
   - Checking 状态下 ShowCheckingAnimation = true
   - IsCheckingUpdate = true 时 ShowCheckingAnimation = true
   - 切换到非 Checking 状态时回到 false
   - IsCheckingUpdate = false 时回到 false

## Edge Cases

### 无网络环境

1. 断开网络连接后启动程序
2. **Expected:** spinner 正常显示，5 秒延迟后开始检查，检查失败后动画停止并显示错误状态

## Failure Signals

- 启动后 spinner 不出现 → 检查 ShowCheckingAnimation 属性绑定
- Spinner 不停旋转 → 检查 Checking 状态是否正确重置
- 编译错误 → 检查 Storyboard 命名空间和 DataTrigger 绑定路径

## Not Proven By This UAT

- Spinner 旋转流畅度（帧率）未做性能测试
- 极端低配设备上动画表现未测试
- 高 DPI/4K 显示器下 spinner 清晰度未验证

## Notes for Tester

- NuGet restore 在当前环境有已知的 "Value cannot be null (path1)" 错误，这是 dotnet SDK 与项目配置的预存问题，与本次变更无关。使用 --no-restore 参数可正常构建和测试。
- Spinner 使用虚线描边（StrokeDashArray）产生视觉旋转效果，不是实心圆旋转。
