using System;
using System.Windows;
using System.Windows.Forms.Integration;

namespace TanjiWPF
{
    public static class WindowExtensions
    {
        public static void Show(this Window window, bool enableModelessKeyboardInterop)
        {
            if (enableModelessKeyboardInterop)
                ElementHost.EnableModelessKeyboardInterop(window);
            window.Show();
        }
    }
}
