using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowTextExtractor
{
    public class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, uint lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        const uint BM_GETCHECK = 0x00F0;
        const uint BM_SETCHECK = 0x00F1;
        const uint BST_CHECKED = 0x0001;
        const uint BST_UNCHECKED = 0x0000;

        public static void Main(string[] args)
        {
            // 写死的参数
            string handle = "205930588";
            IntPtr hWnd = new IntPtr(Convert.ToInt32(handle)); // 字符串句柄转成句柄变量

            // 将所有复选框设置为未选中状态
            bool result = DisableAllCheckBoxes(hWnd);
            if (result)
            {
                Console.WriteLine("操作成功");
            }
            else
            {
                Console.WriteLine("操作失败");
            }
            Console.ReadLine();
        }

        public static bool DisableAllCheckBoxes(IntPtr hWnd)
        {
            bool operationResult = false;

            EnumWindowProc callback = (childHandle, parameter) =>
            {
                StringBuilder classText = new StringBuilder(256);
                GetClassName(childHandle, classText, classText.Capacity);

                Console.WriteLine($"找到控件，句柄: {childHandle}, 类名: {classText}");

                if (classText.ToString() == "Button" || classText.ToString().Contains("BUTTON"))
                {
                    // 获取当前复选框状态
                    IntPtr checkState = SendMessage(childHandle, BM_GETCHECK, IntPtr.Zero, IntPtr.Zero);
                    Console.WriteLine($"找到复选框，当前状态: {checkState}");

                    // 设置复选框状态为未选中
                    if (checkState != (IntPtr)BST_UNCHECKED)
                    {
                        SendMessage(childHandle, BM_SETCHECK, (IntPtr)BST_UNCHECKED, IntPtr.Zero);
                        IntPtr newCheckState = SendMessage(childHandle, BM_GETCHECK, IntPtr.Zero, IntPtr.Zero);
                        Console.WriteLine($"新状态: {newCheckState}");
                        operationResult = (newCheckState == (IntPtr)BST_UNCHECKED);
                    }
                }

                return true; // 继续枚举
            };

            EnumChildWindows(hWnd, callback, IntPtr.Zero);
            return operationResult;
        }
    }
}
