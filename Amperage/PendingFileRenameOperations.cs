using Microsoft.Win32;
using System;

namespace Amperage
{
    internal static class PendingFileRenameOperations
    {
        internal static void FlagFilesForDeletion(string[] filePaths)
        {
            using (var rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager", true))
            {
                var pending = rk.GetValue("PendingFileRenameOperations");
                var capacityExtension = 2 * filePaths.Length;
                string[] newPending;
                int startAt;
                if (pending != null)
                {
                    newPending = (string[])pending;
                    startAt = newPending.Length;
                    Array.Resize(ref newPending, newPending.Length + capacityExtension);
                }
                else
                {
                    newPending = new string[capacityExtension];
                    startAt = 0;
                }

                for (int i = 0; i < filePaths.Length; i++)
                {
                    newPending[startAt + (i * 2)] = @"\??\" + filePaths[i];
                    newPending[startAt + (i * 2) + 1] = "";
                }

                rk.SetValue("PendingFileRenameOperations", newPending);
            }
        }
    }
}
