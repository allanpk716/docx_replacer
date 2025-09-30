using System;
using System.Windows.Input;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 实现ICommand接口的通用命令类
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">执行的操作</param>
        /// <param name="canExecute">是否可以执行的判断</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        /// <summary>
        /// 判断命令是否可以执行
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>是否可以执行</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }
        
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="parameter">命令参数</param>
        public void Execute(object? parameter)
        {
            _execute();
        }
        
        /// <summary>
        /// 可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        /// <summary>
        /// 触发可执行状态改变
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
    
    /// <summary>
    /// 带参数的通用命令类
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">执行的操作</param>
        /// <param name="canExecute">是否可以执行的判断</param>
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        /// <summary>
        /// 判断命令是否可以执行
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>是否可以执行</returns>
        public bool CanExecute(object? parameter)
        {
            if (parameter == null) return _canExecute?.Invoke(default!) ?? true;
            return _canExecute?.Invoke((T)parameter) ?? true;
        }
        
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="parameter">命令参数</param>
        public void Execute(object? parameter)
        {
            if (parameter == null)
            {
                _execute(default!);
            }
            else
            {
                _execute((T)parameter);
            }
        }
        
        /// <summary>
        /// 可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        /// <summary>
        /// 触发可执行状态改变
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}