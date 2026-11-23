using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SibmExambro
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt
        private const int VK_F4 = 0x73;
        private const int VK_F11 = 0x7A;
        private const int VK_F12 = 0x7B;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_APPS = 0x5D;

        // Keys for shortcuts
        private const int VK_X = 0x58; // Ctrl + X
        private const int VK_S = 0x53; // Ctrl + S + I + B + M
        private const int VK_I = 0x49;
        private const int VK_B = 0x42;
        private const int VK_M = 0x4D;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        // Callback Actions
        public Action OnAdminExitTriggered; // Ctrl + S + I + B + M
        public Action OnSupervisorEmergencyExitTriggered; // Ctrl + X

        private string _ctrlSequence = "";

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Install()
        {
            if (_hookId == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                int vk = kbd.vkCode;
                bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool altDown = (kbd.flags & 0x20) != 0;
                bool ctrlDown = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
                bool shiftDown = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

                if (ctrlDown)
                {
                    if (isKeyDown)
                    {
                        // 1. Deteksi Keluar Darurat Pengawas: Ctrl + X
                        if (vk == VK_X)
                        {
                            if (OnSupervisorEmergencyExitTriggered != null)
                            {
                                OnSupervisorEmergencyExitTriggered();
                            }
                            return (IntPtr)1;
                        }

                        // 2. Deteksi Keluar Admin Statis: Ctrl + I + T
                        if (vk >= 0x41 && vk <= 0x5A) // Karakter huruf A-Z
                        {
                            _ctrlSequence += (char)vk;
                            if (_ctrlSequence.Length > 6)
                            {
                                _ctrlSequence = _ctrlSequence.Substring(_ctrlSequence.Length - 6);
                            }

                            bool iDown = (GetKeyState(VK_I) & 0x8000) != 0;
                            bool tDown = (GetKeyState(0x54) & 0x8000) != 0; // VK_T

                            // Terpicu jika tombol I dan T ditekan bersamaan atau berurutan saat Ctrl aktif
                            if ((iDown && tDown) || _ctrlSequence.EndsWith("IT") || (vk == 0x54 && iDown) || (vk == VK_I && tDown))
                            {
                                _ctrlSequence = "";
                                if (OnAdminExitTriggered != null)
                                {
                                    OnAdminExitTriggered();
                                }
                                return (IntPtr)1;
                            }
                        }
                    }
                }
                else
                {
                    _ctrlSequence = "";
                }

                // 3. Blokir Tombol Alt + Tab
                if (altDown && vk == VK_TAB)
                {
                    return (IntPtr)1;
                }

                // 4. Blokir Tombol Alt + Esc
                if (altDown && vk == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 5. Blokir Tombol Alt + F4
                if (altDown && vk == VK_F4)
                {
                    return (IntPtr)1;
                }

                // 6. Blokir Tombol Windows (Win Key Kiri & Kanan) - Cegah Win+Tab, Win+D, Win+R, dll.
                if (vk == VK_LWIN || vk == VK_RWIN)
                {
                    return (IntPtr)1;
                }

                // 7. Blokir Ctrl + Esc (Start menu)
                if (ctrlDown && vk == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 8. Blokir Ctrl + Shift + Esc (Shortcut Task Manager)
                if (ctrlDown && shiftDown && vk == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 9. Blokir Tombol Context Menu / Apps Key
                if (vk == VK_APPS)
                {
                    return (IntPtr)1;
                }

                // 10. Blokir F11 (Fullscreen browser toggle) & F12 (DevTools)
                if (vk == VK_F11 || vk == VK_F12)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
        }
    }
}
