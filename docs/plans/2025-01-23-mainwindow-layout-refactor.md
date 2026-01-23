# 主界面布局重构实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 重构 DocuFiller 主界面布局，简化结构并整合功能入口

**架构：** 修改 MainWindow.xaml，移除冗余Tab页和菜单栏，将审核清理和工具功能整合为新的Tab页

**技术栈：** WPF, XAML, C#, .NET 8

---

## Task 1: 简化Grid行定义并移除菜单栏

**文件：**
- Modify: `MainWindow.xaml:13-20` (Grid.RowDefinitions)
- Modify: `MainWindow.xaml:22-27` (Menu元素)

**Step 1: 修改Grid.RowDefinitions为2行**

将第14-20行的5行定义简化为2行：

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>  <!-- 标题栏 -->
    <RowDefinition Height="*"/>     <!-- TabControl -->
</Grid.RowDefinitions>
```

**Step 2: 删除Menu元素**

删除第22-27行的整个Menu元素：
```xml
<!-- 删除这部分 -->
<Menu Grid.Row="0">
    <MenuItem Header="工具">
        <MenuItem Header="审核清理" Command="{Binding OpenCleanupCommand}"/>
    </MenuItem>
</Menu>
```

**Step 3: 删除UpdateBannerView元素**

删除第29-33行的UpdateBannerView元素：
```xml
<!-- 删除这部分 -->
<views:UpdateBannerView Grid.Row="1"
                        DataContext="{Binding UpdateBanner}"
                        Visibility="{Binding UpdateBanner.IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="0,0,0,10"/>
```

**Step 4: 调整标题栏Grid.Row**

将第36行的 `Grid.Row="2"` 改为 `Grid.Row="0"`：
```xml
<StackPanel Grid.Row="0" HorizontalAlignment="Center" Margin="0,0,0,25">
```

**Step 5: 删除标题栏下方的功能链接区域**

删除第43-69行的整个功能链接StackPanel：
```xml
<!-- 删除这部分 -->
<StackPanel HorizontalAlignment="Center" Orientation="Horizontal" Margin="0,0,0,10">
    <TextBlock Margin="0,0,30,0">
        <Hyperlink x:Name="KeywordEditorHyperlink" ...>
        ...
    </Hyperlink>
    </TextBlock>
    ...
</StackPanel>
```

**Step 6: 调整TabControl的Grid.Row**

将第73行的 `Grid.Row="2"` 改为 `Grid.Row="1"`：
```xml
<TabControl Grid.Row="1" Margin="0,0,0,10" FontSize="16">
```

**Step 7: 编译验证**

```bash
dotnet build
```

预期输出：编译成功，可能有警告但无错误

**Step 8: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): simplify grid layout and remove menu banner"
```

---

## Task 2: 重命名"文件设置"Tab页为"关键词替换"

**文件：**
- Modify: `MainWindow.xaml:75-76` (TabItem Header)

**Step 1: 修改TabItem Header**

将第76行的Header从"文件设置"改为"关键词替换"：
```xml
<TabItem Header="关键词替换" FontSize="16">
```

**Step 2: 编译验证**

```bash
dotnet build
```

**Step 3: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): rename tab from '文件设置' to '关键词替换'"
```

---

## Task 3: 删除"数据预览"Tab页

**文件：**
- Modify: `MainWindow.xaml:253-334` (数据预览TabItem)

**Step 1: 删除数据预览TabItem元素**

删除第253-334行的整个"数据预览"TabItem：
```xml
<!-- 删除这部分 -->
<TabItem Header="数据预览" FontSize="16">
    ...
</TabItem>
```

**Step 2: 编译验证**

```bash
dotnet build
```

**Step 3: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): remove '数据预览' tab"
```

---

## Task 4: 删除"内容控件"Tab页

**文件：**
- Modify: `MainWindow.xaml:336-368` (内容控件TabItem)

**Step 1: 删除内容控件TabItem元素**

删除第336-368行的整个"内容控件"TabItem：
```xml
<!-- 删除这部分 -->
<TabItem Header="内容控件" FontSize="16">
    ...
</TabItem>
```

**Step 2: 编译验证**

```bash
dotnet build
```

**Step 3: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): remove '内容控件' tab"
```

---

## Task 5: 将进度显示区域移入"关键词替换"Tab页

**文件：**
- Modify: `MainWindow.xaml` (TabControl内部)

**Step 1: 定位进度显示区域**

找到进度显示区域的GroupBox（原第372-402行）：
```xml
<!-- 进度显示区域 -->
<GroupBox Grid.Row="3" Header="处理进度" ...>
    ...
</GroupBox>
```

**Step 2: 将进度显示移入"关键词替换"Tab页**

将进度显示区域的GroupBox移动到"关键词替换"TabItem的</ScrollViewer>标签之前（约第250行位置），并移除 `Grid.Row="3"`：

```xml
<TabItem Header="关键词替换" FontSize="16">
    <ScrollViewer ...>
        ...
    </ScrollViewer>

    <!-- 进度显示区域 -->
    <GroupBox Header="处理进度" Style="{StaticResource GroupBoxStyle}" Margin="10,0,0,10" FontSize="16">
        <Grid Margin="15">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <Grid Grid.Row="0" Margin="0,0,0,5">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0"
                           Text="{Binding ProgressMessage}"
                           VerticalAlignment="Center" FontSize="14"/>

                <TextBlock Grid.Column="1"
                           Text="{Binding ProgressText}"
                           VerticalAlignment="Center"
                           FontWeight="Bold" FontSize="14"/>
            </Grid>

            <ProgressBar Grid.Row="1"
                         Value="{Binding ProgressPercentage}"
                         Maximum="100"
                         Height="25"
                         Background="#ECF0F1"
                         Foreground="#3498DB"/>
        </Grid>
    </GroupBox>
</TabItem>
```

**Step 3: 包裹TabItem内容为StackPanel**

由于TabItem内现在有多个元素（ScrollViewer + GroupBox），需要将它们包裹在StackPanel中：

```xml
<TabItem Header="关键词替换" FontSize="16">
    <StackPanel>
        <ScrollViewer ...>
            ...
        </ScrollViewer>

        <!-- 进度显示区域 -->
        <GroupBox ...>
            ...
        </GroupBox>

        <!-- 操作按钮区域 -->
        <Grid ...>
            ...
        </Grid>
    </StackPanel>
</TabItem>
```

**Step 4: 编译验证**

```bash
dotnet build
```

**Step 5: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): move progress bar into '关键词替换' tab"
```

---

## Task 6: 将操作按钮区域移入"关键词替换"Tab页

**文件：**
- Modify: `MainWindow.xaml` (TabControl内部)

**Step 1: 定位操作按钮区域**

找到操作按钮区域的Grid（原第405-427行）：
```xml
<!-- 操作按钮区域 -->
<Grid Grid.Row="4">
    ...
</Grid>
```

**Step 2: 将操作按钮移入"关键词替换"Tab页**

将操作按钮区域的Grid移动到进度显示GroupBox之后，移除 `Grid.Row="4"`，添加Margin：

```xml
<!-- 操作按钮区域 -->
<Grid Margin="10,10,0,0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>

    <Button Grid.Column="1" Content="开始处理"
            Style="{StaticResource PrimaryButton}"
            Command="{Binding StartProcessCommand}"
            Width="120" Height="45" FontSize="16" Margin="0,0,15,0"/>

    <Button Grid.Column="2" Content="取消处理"
            Style="{StaticResource ProcessButton}"
            Command="{Binding CancelProcessCommand}"
            Width="120" Height="45" FontSize="16" Margin="0,0,15,0"/>

    <Button Grid.Column="3" Content="退出"
            Style="{StaticResource ProcessButton}"
            Command="{Binding ExitCommand}"
            Width="100" Height="45" FontSize="16"/>
</Grid>
```

**Step 3: 编译验证**

```bash
dotnet build
```

**Step 4: 提交**

```bash
git add MainWindow.xaml
git commit -m "refactor(main): move action buttons into '关键词替换' tab"
```

---

## Task 7: 添加"审核清理"Tab页

**文件：**
- Modify: `MainWindow.xaml` (TabControl内部)
- Read: `Views/CleanupWindow.xaml` (参考布局)

**Step 1: 读取CleanupWindow布局作为参考**

```bash
# 查看CleanupWindow的完整布局结构
# 位于 Views/CleanupWindow.xaml
```

**Step 2: 在TabControl中添加"审核清理"TabItem**

在"关键词替换"TabItem之后添加新的TabItem：

```xml
<!-- 审核清理选项卡 -->
<TabItem Header="审核清理" FontSize="16">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 拖放区域 + 文件列表 -->
        <Border Grid.Row="0" Grid.RowSpan="2" BorderBrush="#DDDDDD" BorderThickness="1" Padding="10" CornerRadius="5">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- 拖放提示 -->
                <Border Grid.Row="0" x:Name="CleanupDropZoneBorder"
                         BorderBrush="#CCCCCC" BorderThickness="2"
                         Padding="30" Background="#F9F9F9"
                         AllowDrop="True"
                         Margin="0,0,0,10">
                    <StackPanel HorizontalAlignment="Center">
                        <TextBlock Text="将文件或文件夹拖放到此处" FontSize="14" Foreground="#666666" HorizontalAlignment="Center"/>
                        <TextBlock Text="支持 .docx 文件" FontSize="12" Foreground="#999999" HorizontalAlignment="Center" Margin="0,5,0,0"/>
                    </StackPanel>
                </Border>

                <!-- 文件列表 -->
                <Grid Grid.Row="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <ListView Grid.Column="0" ItemsSource="{Binding CleanupFileItems}" SelectionMode="Extended">
                        <ListView.View>
                            <GridView>
                                <GridViewColumn Width="40" Header=""/>
                                <GridViewColumn Width="300" Header="文件名" DisplayMemberBinding="{Binding FileName}"/>
                                <GridViewColumn Width="100" Header="大小" DisplayMemberBinding="{Binding FileSizeDisplay}"/>
                                <GridViewColumn Width="150" Header="状态" DisplayMemberBinding="{Binding StatusDisplay}"/>
                            </GridView>
                        </ListView.View>
                    </ListView>

                    <StackPanel Grid.Column="1" Margin="10,0,0,0">
                        <Button Content="移除选中" Width="100" Height="30" Margin="0,0,0,5" Command="{Binding RemoveSelectedCleanupCommand}"/>
                        <Button Content="清空列表" Width="100" Height="30" Command="{Binding ClearCleanupListCommand}"/>
                    </StackPanel>
                </Grid>
            </Grid>
        </Border>

        <!-- 进度 -->
        <StackPanel Grid.Row="2" Margin="0,10,0,10">
            <TextBlock Text="{Binding CleanupProgressStatus}" Margin="0,0,0,5"/>
            <ProgressBar Height="25" Value="{Binding CleanupProgressPercent}" Maximum="100"/>
        </StackPanel>

        <!-- 按钮 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="开始清理" Width="120" Height="35" Margin="0,0,10,0"
                    IsEnabled="{Binding CanStartCleanup}" Command="{Binding StartCleanupCommand}"
                    Style="{StaticResource PrimaryButton}"/>
            <Button Content="关闭" Width="100" Height="35" Command="{Binding CloseCleanupCommand}"
                    Style="{StaticResource ProcessButton}"/>
        </StackPanel>
    </Grid>
</TabItem>
```

**Step 3: 编译验证**

```bash
dotnet build
```

预期：可能有编译错误，因为ViewModel中还没有相关属性和命令

**Step 4: 提交**

```bash
git add MainWindow.xaml
git commit -m "feat(main): add '审核清理' tab with cleanup UI"
```

---

## Task 8: 添加"工具"Tab页

**文件：**
- Modify: `MainWindow.xaml` (TabControl内部)

**Step 1: 在TabControl中添加"工具"TabItem**

在"审核清理"TabItem之后添加新的TabItem，采用卡片式布局：

```xml
<!-- 工具选项卡 -->
<TabItem Header="工具" FontSize="16">
    <Grid Margin="30">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 关键词编辑器 -->
        <Border Grid.Row="0" BorderBrush="#BDC3C7" BorderThickness="1" CornerRadius="8" Margin="0,0,0,15"
                Background="White" Cursor="Hand">
            <Border.InputBindings>
                <MouseBinding Command="{Binding OpenKeywordEditorCommand}" MouseAction="LeftClick"/>
            </Border.InputBindings>
            <Grid Margin="20">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <!-- 图标/emoji -->
                <TextBlock Grid.Column="0" Text="📝" FontSize="48" VerticalAlignment="Center" Margin="0,0,20,0"/>

                <!-- 内容 -->
                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                    <TextBlock Text="关键词编辑器" FontSize="20" FontWeight="Bold" Foreground="#2C3E50" Margin="0,0,0,5"/>
                    <TextBlock Text="管理和编辑文档关键词" FontSize="14" Foreground="#7F8C8D"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- JSON转Excel转换工具 -->
        <Border Grid.Row="1" BorderBrush="#BDC3C7" BorderThickness="1" CornerRadius="8" Margin="0,0,0,15"
                Background="White" Cursor="Hand">
            <Border.InputBindings>
                <MouseBinding Command="{Binding OpenConverterCommand}" MouseAction="LeftClick"/>
            </Border.InputBindings>
            <Grid Margin="20">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0" Text="🔄" FontSize="48" VerticalAlignment="Center" Margin="0,0,20,0"/>

                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                    <TextBlock Text="JSON转Excel转换工具" FontSize="20" FontWeight="Bold" Foreground="#2C3E50" Margin="0,0,0,5"/>
                    <TextBlock Text="将JSON数据文件转换为Excel格式" FontSize="14" Foreground="#7F8C8D"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 检查更新 -->
        <Border Grid.Row="2" BorderBrush="#BDC3C7" BorderThickness="1" CornerRadius="8"
                Background="White" Cursor="Hand">
            <Border.InputBindings>
                <MouseBinding Command="{Binding CheckForUpdateCommand}" MouseAction="LeftClick"/>
            </Border.InputBindings>
            <Grid Margin="20">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0" Text="🔔" FontSize="48" VerticalAlignment="Center" Margin="0,0,20,0"/>

                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                    <TextBlock Text="检查更新" FontSize="20" FontWeight="Bold" Foreground="#2C3E50" Margin="0,0,0,5"/>
                    <TextBlock Text="检查并下载最新版本" FontSize="14" Foreground="#7F8C8D"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</TabItem>
```

**Step 2: 编译验证**

```bash
dotnet build
```

**Step 3: 提交**

```bash
git add MainWindow.xaml
git commit -m "feat(main): add '工具' tab with utility shortcuts"
```

---

## Task 9: 更新ViewModel以支持新功能

**文件：**
- Modify: `ViewModels/MainViewModel.cs`

**Step 1: 添加审核清理相关属性**

在MainViewModel中添加以下属性：

```csharp
// 清理文件列表
private ObservableCollection<CleanupFileItem> _cleanupFileItems = new();
public ObservableCollection<CleanupFileItem> CleanupFileItems
{
    get => _cleanupFileItems;
    set => SetProperty(ref _cleanupFileItems, value);
}

// 清理进度状态
private string _cleanupProgressStatus = "等待开始...";
public string CleanupProgressStatus
{
    get => _cleanupProgressStatus;
    set => SetProperty(ref _cleanupProgressStatus, value);
}

// 清理进度百分比
private int _cleanupProgressPercent;
public int CleanupProgressPercent
{
    get => _cleanupProgressPercent;
    set => SetProperty(ref _cleanupProgressPercent, value);
}

// 是否可以开始清理
private bool _canStartCleanup = true;
public bool CanStartCleanup
{
    get => _canStartCleanup;
    set => SetProperty(ref _canStartCleanup, value);
}
```

**Step 2: 添加清理相关命令**

```csharp
public ICommand RemoveSelectedCleanupCommand { get; }
public ICommand ClearCleanupListCommand { get; }
public ICommand StartCleanupCommand { get; }
public ICommand CloseCleanupCommand { get; }
```

**Step 3: 在构造函数中初始化命令**

```csharp
RemoveSelectedCleanupCommand = new RelayCommand(RemoveSelectedCleanup);
ClearCleanupListCommand = new RelayCommand(ClearCleanupList);
StartCleanupCommand = new RelayCommand(StartCleanup, () => CanStartCleanup);
CloseCleanupCommand = new RelayCommand(CloseCleanup);
```

**Step 4: 添加命令实现方法**

```csharp
private void RemoveSelectedCleanup()
{
    // TODO: 实现移除选中项逻辑
}

private void ClearCleanupList()
{
    CleanupFileItems.Clear();
}

private void StartCleanup()
{
    // TODO: 调用清理服务
}

private void CloseCleanup()
{
    // 重置清理状态
    CleanupFileItems.Clear();
    CleanupProgressStatus = "等待开始...";
    CleanupProgressPercent = 0;
}
```

**Step 5: 添加工具菜单命令（如果不存在）**

确认以下命令存在或添加：

```csharp
public ICommand OpenKeywordEditorCommand { get; }
public ICommand OpenConverterCommand { get; }
public ICommand CheckForUpdateCommand { get; }
```

**Step 6: 编译验证**

```bash
dotnet build
```

**Step 7: 提交**

```bash
git add ViewModels/MainViewModel.cs
git commit -m "feat(viewModel): add cleanup and tools command support"
```

---

## Task 10: 移除不再使用的代码

**文件：**
- Modify: `ViewModels/MainViewModel.cs`
- Modify: `MainWindow.xaml.cs`
- Modify: `App.xaml.cs` (如果有UpdateBanner相关注册)

**Step 1: 移除OpenCleanupCommand**

在MainViewModel中查找并删除或注释：
```csharp
// public ICommand OpenCleanupCommand { get; }  // 不再需要，清理功能直接在Tab中
```

**Step 2: 移除UpdateBanner相关属性和命令**

查找并删除UpdateBanner相关的属性和命令：
```csharp
// 删除 UpdateBanner 属性
```

**Step 3: 清理MainWindow.xaml.cs中的事件处理器**

如果`KeywordEditorHyperlink_Click`、`ConverterHyperlink_Click`、`CheckForUpdateHyperlink_Click`等事件处理器不再被使用，可以保留它们（因为新的Tab页使用Command绑定）

**Step 4: 移除CleanupWindow（可选）**

如果确认不再需要独立的清理窗口，可以删除：
- `Views/CleanupWindow.xaml`
- `Views/CleanupWindow.xaml.cs`

**注意：** 如果保留CleanupWindow以备后用，可以跳过此步骤

**Step 5: 编译验证**

```bash
dotnet build
```

**Step 6: 提交**

```bash
git add ViewModels/MainViewModel.cs MainWindow.xaml.cs
git commit -m "refactor: remove unused cleanup window and banner code"
```

---

## Task 11: 测试验证

**文件：**
- Build: `DocuFiller.csproj`

**Step 1: 完整编译**

```bash
dotnet build -c Release
```

预期输出：编译成功，无错误

**Step 2: 运行应用程序**

```bash
dotnet run
```

**Step 3: 手动测试清单**

1. **界面布局检查**
   - [ ] 标题栏显示"Word文档批量填充工具"
   - [ ] 下方有3个Tab页：关键词替换、审核清理、工具
   - [ ] 没有菜单栏
   - [ ] 没有更新横幅

2. **关键词替换Tab页**
   - [ ] 可以选择模板文件
   - [ ] 可以选择数据文件
   - [ ] 可以设置输出目录
   - [ ] 进度条显示在Tab页底部
   - [ ] 操作按钮（开始处理、取消处理、退出）显示在Tab页底部

3. **审核清理Tab页**
   - [ ] 拖放区域显示正常
   - [ ] 文件列表显示正常
   - [ ] 进度条显示正常
   - [ ] 操作按钮（开始清理、关闭）显示正常

4. **工具Tab页**
   - [ ] 显示3个工具卡片
   - [ ] 点击关键词编辑器可以打开对应窗口
   - [ ] 点击JSON转Excel转换工具可以打开对应窗口
   - [ ] 点击检查更新可以执行更新检查

**Step 4: 提交最终版本**

```bash
git add .
git commit -m "refactor(main): complete layout refactor - verified and working"
```

---

## 总结

本实施计划将主界面布局重构分为11个任务：

1. 简化Grid布局并移除菜单栏/横幅
2. 重命名Tab页
3-4. 删除冗余Tab页
5-6. 移动进度和按钮到Tab页内
7-8. 添加新的Tab页
9. 更新ViewModel支持
10. 清理旧代码
11. 测试验证

每个任务都包含详细的代码修改和验证步骤，确保可以逐步完成并随时回滚。
