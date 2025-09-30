using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// JSON关键词编辑器ViewModel
    /// </summary>
    public class JsonEditorViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IJsonEditorService _jsonEditorService;
        private readonly IKeywordValidationService _validationService;
        private readonly ILogger<JsonEditorViewModel> _logger;

        // 私有字段
        private JsonProjectModel? _currentProject;
        private JsonKeywordItem? _selectedKeyword;
        private string _jsonPreview = string.Empty;
        private string _statusMessage = "就绪";
        private bool _hasUnsavedChanges = false;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private ValidationResult _validationResult = new ValidationResult { IsValid = true };

        // 集合属性
        public ObservableCollection<JsonKeywordItem> Keywords { get; } = new ObservableCollection<JsonKeywordItem>();
        public ObservableCollection<JsonKeywordItem> FilteredKeywords { get; } = new ObservableCollection<JsonKeywordItem>();
        public ObservableCollection<string> ValidationErrors { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ValidationWarnings { get; } = new ObservableCollection<string>();

        public JsonEditorViewModel(
            IJsonEditorService jsonEditorService,
            IKeywordValidationService validationService,
            ILogger<JsonEditorViewModel> logger)
        {
            _jsonEditorService = jsonEditorService ?? throw new ArgumentNullException(nameof(jsonEditorService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
            InitializeNewProject();
        }
        
        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public void Initialize()
        {
            // 执行初始化逻辑
            Console.WriteLine("[DEBUG] JsonEditorViewModel 初始化完成");
            
            // 可以在这里添加其他初始化逻辑
            // 例如：加载默认设置、检查文件权限等
        }

        #region 属性

        public JsonProjectModel? CurrentProject
        {
            get => _currentProject;
            set
            {
                if (SetProperty(ref _currentProject, value))
                {
                    OnProjectChanged();
                }
            }
        }

        public JsonKeywordItem? SelectedKeyword
        {
            get => _selectedKeyword;
            set
            {
                if (SetProperty(ref _selectedKeyword, value))
                {
                    OnPropertyChanged(nameof(CanEditKeyword));
                    OnPropertyChanged(nameof(CanDeleteKeyword));
                }
            }
        }

        public string JsonPreview
        {
            get => _jsonPreview;
            set => SetProperty(ref _jsonPreview, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanPerformFileOperations));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterKeywords();
                }
            }
        }

        public ValidationResult ValidationResult
        {
            get => _validationResult;
            set
            {
                if (SetProperty(ref _validationResult, value))
                {
                    UpdateValidationDisplay();
                }
            }
        }

        // 计算属性
        public string WindowTitle => $"JSON关键词编辑器 - {CurrentProject?.ProjectName ?? "未命名项目"}{(HasUnsavedChanges ? " *" : "")}";
        public bool CanEditKeyword => SelectedKeyword != null;
        public bool CanDeleteKeyword => SelectedKeyword != null && Keywords.Count > 0;
        public bool CanPerformFileOperations => !IsLoading;
        public int KeywordCount => Keywords.Count;
        public bool HasValidationErrors => ValidationErrors.Count > 0;
        public bool HasValidationWarnings => ValidationWarnings.Count > 0;

        #endregion

        #region 命令

        public ICommand NewProjectCommand { get; private set; } = null!;
        public ICommand OpenProjectCommand { get; private set; } = null!;
        public ICommand SaveProjectCommand { get; private set; } = null!;
        public ICommand SaveAsProjectCommand { get; private set; } = null!;
        public ICommand AddKeywordCommand { get; private set; } = null!;
        public ICommand EditKeywordCommand { get; private set; } = null!;
        public ICommand DeleteKeywordCommand { get; private set; } = null!;
        public ICommand MoveUpCommand { get; private set; } = null!;
        public ICommand MoveDownCommand { get; private set; } = null!;
        public ICommand ValidateAllCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand ExportJsonCommand { get; private set; } = null!;
        public ICommand ImportJsonCommand { get; private set; } = null!;
        
        // SaveCommand 属性，指向 SaveProjectCommand
        public ICommand SaveCommand => SaveProjectCommand;

        private void InitializeCommands()
        {
            NewProjectCommand = new RelayCommand(async () => await NewProjectAsync(), () => CanPerformFileOperations);
            OpenProjectCommand = new RelayCommand(async () => await OpenProjectAsync(), () => CanPerformFileOperations);
            SaveProjectCommand = new RelayCommand(async () => await SaveProjectAsync(), () => CanPerformFileOperations && HasUnsavedChanges);
            SaveAsProjectCommand = new RelayCommand(async () => await SaveAsProjectAsync(), () => CanPerformFileOperations);
            AddKeywordCommand = new RelayCommand(AddKeyword);
            EditKeywordCommand = new RelayCommand(EditKeyword, () => CanEditKeyword);
            DeleteKeywordCommand = new RelayCommand(DeleteKeyword, () => CanDeleteKeyword);
            MoveUpCommand = new RelayCommand(MoveKeywordUp, () => CanMoveUp());
            MoveDownCommand = new RelayCommand(MoveKeywordDown, () => CanMoveDown());
            ValidateAllCommand = new RelayCommand(ValidateAll);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrEmpty(SearchText));
            ExportJsonCommand = new RelayCommand(async () => await ExportJsonAsync(), () => CanPerformFileOperations);
            ImportJsonCommand = new RelayCommand(async () => await ImportJsonAsync(), () => CanPerformFileOperations);
        }

        #endregion

        #region 命令实现

        private async Task NewProjectAsync()
        {
            _logger.LogInformation("[调试] 创建新项目");

            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show("当前项目有未保存的更改，是否保存？", "确认", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (!await SaveProjectAsync())
                        return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            InitializeNewProject();
            StatusMessage = "新项目已创建";
        }

        private async Task OpenProjectAsync()
        {
            _logger.LogInformation("[调试] 打开项目文件");

            var openFileDialog = new OpenFileDialog
            {
                Title = "打开JSON项目文件",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadProjectAsync(openFileDialog.FileName);
            }
        }

        private async Task<bool> SaveProjectAsync()
        {
            if (string.IsNullOrEmpty(CurrentProject?.FilePath))
            {
                return await SaveAsProjectAsync();
            }

            return await SaveProjectToFileAsync(CurrentProject.FilePath);
        }

        private async Task<bool> SaveAsProjectAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存JSON项目文件",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"{CurrentProject?.ProjectName ?? "新项目"}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return await SaveProjectToFileAsync(saveFileDialog.FileName);
            }

            return false;
        }

        private void AddKeyword()
        {
            _logger.LogInformation("[调试] 添加新关键词");

            var newKeyword = new JsonKeywordItem
            {
                Key = "#新关键词#",
                Value = "新值",
                SourceFile = "示例文件.docx"
            };

            CurrentProject?.AddKeyword(newKeyword);
            Keywords.Add(newKeyword);
            FilteredKeywords.Add(newKeyword);
            SelectedKeyword = newKeyword;

            UpdateJsonPreview();
            MarkAsChanged();
            StatusMessage = "已添加新关键词";
        }

        private void EditKeyword()
        {
            if (SelectedKeyword == null) return;

            _logger.LogInformation($"[调试] 编辑关键词: {SelectedKeyword.Key}");
            // 这里可以打开编辑对话框或直接在界面中编辑
            StatusMessage = $"正在编辑关键词: {SelectedKeyword.Key}";
        }

        private void DeleteKeyword()
        {
            if (SelectedKeyword == null) return;

            _logger.LogInformation($"[调试] 删除关键词: {SelectedKeyword.Key}");

            var result = MessageBox.Show($"确定要删除关键词 '{SelectedKeyword.Key}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var keywordToDelete = SelectedKeyword;
                if (keywordToDelete != null)
                {
                    CurrentProject?.RemoveKeyword(keywordToDelete);
                    Keywords.Remove(keywordToDelete);
                    FilteredKeywords.Remove(keywordToDelete);

                    UpdateJsonPreview();
                    MarkAsChanged();
                    StatusMessage = $"已删除关键词: {keywordToDelete.Key}";
                }
            }
        }

        private void MoveKeywordUp()
        {
            if (SelectedKeyword == null) return;

            var index = Keywords.IndexOf(SelectedKeyword);
            if (index > 0)
            {
                Keywords.Move(index, index - 1);
                CurrentProject?.Keywords.Move(index, index - 1);
                FilterKeywords();
                UpdateJsonPreview();
                MarkAsChanged();
            }
        }

        private void MoveKeywordDown()
        {
            if (SelectedKeyword == null) return;

            var index = Keywords.IndexOf(SelectedKeyword);
            if (index < Keywords.Count - 1)
            {
                Keywords.Move(index, index + 1);
                CurrentProject?.Keywords.Move(index, index + 1);
                FilterKeywords();
                UpdateJsonPreview();
                MarkAsChanged();
            }
        }

        private void ValidateAll()
        {
            _logger.LogInformation("[调试] 验证所有关键词");

            var projectValidation = CurrentProject != null ? _jsonEditorService.ValidateProject(CurrentProject) : new ValidationResult { IsValid = false, Errors = { "项目为空" } };
            var keywordValidation = _validationService.ValidateKeywordList(Keywords);

            var combinedResult = new ValidationResult { IsValid = projectValidation.IsValid && keywordValidation.IsValid };
            combinedResult.Errors.AddRange(projectValidation.Errors);
            combinedResult.Errors.AddRange(keywordValidation.Errors);
            combinedResult.Warnings.AddRange(projectValidation.Warnings);
            combinedResult.Warnings.AddRange(keywordValidation.Warnings);

            ValidationResult = combinedResult;
            StatusMessage = ValidationResult.IsValid ? "验证通过" : $"验证失败，发现 {ValidationResult.Errors.Count} 个错误";
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private async Task ExportJsonAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出JSON文件",
                Filter = "JSON文件 (*.json)|*.json",
                DefaultExt = "json",
                FileName = $"{CurrentProject?.ProjectName ?? "导出"}_export.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var jsonContent = _jsonEditorService.FormatJsonString(CurrentProject!);
                    await File.WriteAllTextAsync(saveFileDialog.FileName, jsonContent);
                    StatusMessage = "JSON文件导出成功";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[调试] 导出JSON文件失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ImportJsonAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "导入JSON文件",
                Filter = "JSON文件 (*.json)|*.json",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(openFileDialog.FileName);
                    var importedProject = _jsonEditorService.ParseJsonString(jsonContent);
                    
                    var result = MessageBox.Show("导入的数据将替换当前项目，是否继续？", "确认导入", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CurrentProject = importedProject;
                        StatusMessage = "JSON文件导入成功";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[调试] 导入JSON文件失败");
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 辅助方法

        private void InitializeNewProject()
        {
            CurrentProject = _jsonEditorService.CreateNewProject();
            HasUnsavedChanges = false;
        }

        private async Task LoadProjectAsync(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载项目...";

                var project = await _jsonEditorService.LoadProjectAsync(filePath);
                CurrentProject = project;
                StatusMessage = $"项目加载成功: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[调试] 加载项目失败: {filePath}");
                MessageBox.Show($"加载项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "加载项目失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> SaveProjectToFileAsync(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存项目...";

                if (CurrentProject == null)
                {
                    StatusMessage = "项目为空，无法保存";
                    return false;
                }

                var success = await _jsonEditorService.SaveProjectAsync(CurrentProject, filePath);
                if (success)
                {
                    HasUnsavedChanges = false;
                    StatusMessage = $"项目保存成功: {Path.GetFileName(filePath)}";
                    return true;
                }
                else
                {
                    MessageBox.Show("保存项目失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "保存项目失败";
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[调试] 保存项目失败: {filePath}");
                MessageBox.Show($"保存项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "保存项目失败";
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnProjectChanged()
        {
            if (CurrentProject != null)
            {
                Keywords.Clear();
                foreach (var keyword in CurrentProject.Keywords)
                {
                    Keywords.Add(keyword);
                }

                CurrentProject.PropertyChanged += OnProjectPropertyChanged;
                FilterKeywords();
                UpdateJsonPreview();
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(KeywordCount));
            }
        }

        private void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JsonProjectModel.HasUnsavedChanges))
            {
                HasUnsavedChanges = CurrentProject?.HasUnsavedChanges ?? false;
            }
            else if (e.PropertyName == nameof(JsonProjectModel.ProjectName))
            {
                OnPropertyChanged(nameof(WindowTitle));
                UpdateJsonPreview();
            }
        }

        private void FilterKeywords()
        {
            FilteredKeywords.Clear();

            var filteredItems = string.IsNullOrWhiteSpace(SearchText)
                ? Keywords
                : Keywords.Where(k => 
                    k.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    k.Value.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    k.SourceFile.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var item in filteredItems)
            {
                FilteredKeywords.Add(item);
            }
        }

        private void UpdateJsonPreview()
        {
            try
            {
                JsonPreview = CurrentProject != null ? _jsonEditorService.FormatJsonString(CurrentProject) : "{}"; // 空项目显示空JSON
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[调试] 更新JSON预览失败");
                JsonPreview = "JSON预览生成失败";
            }
        }

        private void UpdateValidationDisplay()
        {
            ValidationErrors.Clear();
            ValidationWarnings.Clear();

            if (ValidationResult != null)
            {
                foreach (var error in ValidationResult.Errors)
                {
                    ValidationErrors.Add(error);
                }

                foreach (var warning in ValidationResult.Warnings)
                {
                    ValidationWarnings.Add(warning);
                }
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(HasValidationWarnings));
        }

        private void MarkAsChanged()
        {
            CurrentProject?.MarkAsChanged();
        }

        private bool CanMoveUp()
        {
            return SelectedKeyword != null && Keywords.IndexOf(SelectedKeyword) > 0;
        }

        private bool CanMoveDown()
        {
            return SelectedKeyword != null && Keywords.IndexOf(SelectedKeyword) < Keywords.Count - 1;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (CurrentProject != null)
            {
                CurrentProject.PropertyChanged -= OnProjectPropertyChanged;
            }
        }

        #endregion
    }
}