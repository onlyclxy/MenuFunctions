using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WindowTextExtractor
{
    public class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SendMessageString(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        const uint WM_SETTEXT = 0x000C;
        const uint WM_GETTEXT = 0x000D;
        const uint WM_GETTEXTLENGTH = 0x000E;

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: WindowTextExtractor <windowHandle> ");
                return;
            }

            string handle = args[0];
            IntPtr hWnd = new IntPtr(Convert.ToInt32(handle)); //字符串句柄转成句柄变量

            //// 示例调用获取控件文本的方法
            //string text = GetTextFromWindow(handle, "WindowsForms10.EDIT.app.0.3c3ecb7_r24_ad1", 27);
            //Console.WriteLine(text);

            for (int i = 1; i <= 27; i++)
            {
                //WindowsForms10.EDIT.app.0.1e35270_r24_ad1 这个可能会变化, 去spy++里找到这个编辑框 ,然后获取他的新类名,改到下面这个地方
                string text = GetTextFromWindow(handle, "WindowsForms10.EDIT.app.0.1e35270_r32_ad1", i);
                if (text!=null && text.StartsWith(".."))
                {
                    Console.WriteLine(text);
                    break;
                }
            }
        }



        public static string GetTextFromWindow(string windowHandle, string className, int instance)
        {
            IntPtr hWnd = new IntPtr(Convert.ToInt32(windowHandle));
            int count = 0;
            string windowText = null;

            EnumWindowProc callback = (childHandle, parameter) =>
            {
                StringBuilder classText = new StringBuilder(256);
                GetClassName(childHandle, classText, classText.Capacity);

                if (classText.ToString() == className)
                {
                    if (count == instance)
                    {
                        int length = (int)SendMessage(childHandle, WM_GETTEXTLENGTH, IntPtr.Zero, null);
                        StringBuilder sb = new StringBuilder(length + 1);
                        SendMessage(childHandle, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                        windowText = sb.ToString();
                        return false; // Stop enumeration
                    }
                    count++;
                }

                return true; // Continue enumeration
            };

            EnumChildWindows(hWnd, callback, IntPtr.Zero);
            return windowText;
        }
    }
}
