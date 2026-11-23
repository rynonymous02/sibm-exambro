using System;
using Microsoft.Win32;

namespace SibmExambro
{
    public static class SystemPolicy
    {
        private const string PolicyKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";

        public static void ApplyLockdown()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(PolicyKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableLockWorkstation", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore if registry permissions are restricted
            }
        }

        public static void Restore()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PolicyKeyPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("DisableTaskMgr", false);
                        key.DeleteValue("DisableChangePassword", false);
                        key.DeleteValue("DisableLockWorkstation", false);
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore
            }
        }
    }
}
