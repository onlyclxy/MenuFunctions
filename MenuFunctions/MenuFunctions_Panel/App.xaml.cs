using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MenuFunctions_Panel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 全局异常处理
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"程序发生未处理的异常:\n\n{ex?.Message}\n\n{ex?.StackTrace}", 
                    "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"UI线程发生未处理的异常:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // 显式创建并显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();
                mainWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建窗口失败:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }
        }
    }
}
