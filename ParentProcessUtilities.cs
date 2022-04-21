using System;
using System.Management;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Youtube2MP3
{
    static class ParentProcessUtilities
    {

        public static IEnumerable<Process> GetChildProcesses()
        {
            Process process = Process.GetCurrentProcess();
            List<Process> children = new List<Process>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));

            foreach (ManagementObject mo in mos.Get())
            {
                try
                {
                    children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
                }
                catch { }
            }

            return children;
        }
    }
}
