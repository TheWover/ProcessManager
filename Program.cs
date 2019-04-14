/** Name: ProcessManager
 * Author: TheWover
 * Description: Displays useful information about processes running on a local or remote machine.
 * 
 * Last Modified: 04/13/2018
 * 
 */

using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Principal;

namespace ProcessManager
{

    class Program
    {
        private struct Arguments
        {
            public string processname;
            public string machinename;
            public bool help;
        }

        static void Main(string[] args)
        {
            //Parse command-line arguments
            Arguments arguments = ParseArgs(args);

            if (args.Length > 0)
            {
                if (arguments.help == true)
                {
                    PrintUsage();
                    Environment.Exit(0);
                }

                Console.WriteLine("{0,-30} {1,-15} {2,-15} {3,-15} {4,-15} {5,-15} {6}", "Process Name", "PID", "PPID", "Arch", "Managed", "Session", "User");

                //If the user specifed that a different machine should be used, then parse for the machine name and run the command.
                if (arguments.machinename != null)
                {
                    try
                    {
                        if (arguments.processname != null)
                            
                            //Enumerate the processes
                            DescribeProcesses(Process.GetProcessesByName(arguments.processname, arguments.machinename));
                        else

                            //Enumerate the processes
                            DescribeProcesses(Process.GetProcesses(arguments.machinename));
                    }
                    catch
                    {
                        Console.WriteLine("Error: Invalid machine name.");

                        Environment.Exit(1);
                    }
                }
                else
                {
                    if (arguments.processname != null)
                        //Enumerate the processes
                        DescribeProcesses(Process.GetProcessesByName(arguments.processname));
                    else
                        //Enumerate the processes
                        DescribeProcesses(Process.GetProcesses());
                }
                
            }
            else
            {
                Console.WriteLine("{0,-30} {1,-15} {2,-15} {3,-15} {4,-15} {5,-15} {6}", "Process Name", "PID", "PPID", "Arch", "Managed", "Session", "User");

                DescribeProcesses(Process.GetProcesses());
            }
        }

        private static Arguments ParseArgs(string[] args)
        {
            Arguments arguments = new Arguments();
            arguments.help = false;
            arguments.machinename = null;
            arguments.processname = null;

            if (args.Length > 0)
            {
                if (args.Contains("--help") || args.Contains("-h"))
                {
                    arguments.help = true;
                }
            }

            //Filter by process name
            if (args.Contains("--name") && args.Length >= 2)
            {
                //The number of the command line argument that specifies the process name
                int nameindex = new System.Collections.Generic.List<string>(args).IndexOf("--name") + 1;

                arguments.processname = args[nameindex];
            }

            //If the user specifed that a different machine should be used, then parse for the machine name and run the command.
            if (args.Contains("--machine") && args.Length >= 2)
            {
                try
                {
                    //The number of the command line argument that specifies the machine name
                    int machineindex = new System.Collections.Generic.List<string>(args).IndexOf("--machine") + 1;

                    arguments.machinename = args[machineindex];
                }
                catch
                {
                    Console.WriteLine("Error: Invalid machine name.");

                    Environment.Exit(1);
                }

            }

            return arguments;
        }

        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("| Process Manager [v0.1]");
            Console.WriteLine("| Copyright (c) 2019 TheWover");
            Console.WriteLine();

            Console.WriteLine("Usage: ProcessManager.exe [machine]");
            Console.WriteLine();

            Console.WriteLine("{0,-5} {1,-20} {2}", "", "-h, --help", "Display this help menu.");
            Console.WriteLine("{0,-5} {1,-20} {2}", "", "--machine", "Specify a machine to query. Machine name or IP Address may be used.");
            Console.WriteLine("{0,-5} {1,-20} {2}", "", "--name", "Filter by a process name.");
            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine();

            Console.WriteLine("ProcessManager.exe");
            Console.WriteLine("ProcessManager.exe --name svchost");
            Console.WriteLine("ProcessManager.exe --machine workstation2");
            Console.WriteLine("ProcessManager.exe --machine 10.30.134.13");
            Console.WriteLine();
        }        

        private static void DescribeProcesses(Process[] processes)
        {
            //
            processes = processes.OrderBy(p => p.Id).ToArray();

            foreach (Process process in processes)
            {

                ProcessDetails details = new ProcessDetails();
                details.name = process.ProcessName;
                details.pid = process.Id;



                Process parent = ParentProcessUtilities.GetParentProcess(process.Id);
                if (parent != null)
                    details.ppid = parent.Id;
                else
                    details.ppid = -1;

                try
                {
                    if (ProcessInspector.IsWow64Process(process))
                        details.arch = "x64";
                    else
                        details.arch = "x86";
                }
                catch
                {
                    details.arch = "*";
                }

                //Determine whether or not the process is managed (has the CLR loaded).
                details.managed = ProcessInspector.IsCLRLoaded(process);

                details.session = process.SessionId;

                details.user = ProcessInspector.GetProcessUser(process);

                Console.WriteLine("{0,-30} {1,-15} {2,-15} {3,-15} {4,-15} {5,-15} {6}", details.name, details.pid, details.ppid, details.arch, details.managed, details.session, details.user);
            }
        }
    }

    public struct ProcessDetails
    {
        public string name;
        public int pid;
        public int ppid;
        public string arch;
        public bool managed;
        public int session;
        public string user;
    }

    public static class ProcessInspector
    {

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool IsWow64Process(System.IntPtr hProcess, out bool lpSystemInfo);


        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <returns>A Process object representing the parent.</returns>
        public static Process GetParentProcess(Process process)
        {
            return ParentProcessUtilities.GetParentProcess(process.Id);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <returns>A Process object representing the parent.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess());
        }

        /// <summary>
        /// Checks whether the process is 64-bit.
        /// </summary>
        /// <returns>Returns true if process is 64-bit, and false if process is 32-bit.</returns>
        public static bool IsWow64Process(Process process)
        {
            bool retVal = false;
            IsWow64Process(process.Handle, out retVal);
            return retVal;
        }

        /// <summary>
        /// Checks whether the process is 64-bit.
        /// </summary>
        /// <returns>Returns true if process is 64-bit, and false if process is 32-bit.</returns>
        public static bool IsWow64Process()
        {
            bool retVal = false;
            IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
            return retVal;
        }

        /// <summary>
        /// Checks if the CLR has been loaded into the specified process by 
        /// looking for loaded modules that contain "mscor" in the name.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>True if the CLR has been loaded. False if it has not.</returns>
        public static bool IsCLRLoaded(Process process)
        {
            try
            {
                var modules = from module in process.Modules.OfType<ProcessModule>()
                              select module;

                return modules.Any(pm => pm.ModuleName.Contains("mscor"));
            }
            //Access was denied
            catch (Win32Exception)
            {
                return false;
            }
            //Process has already exited
            catch (InvalidOperationException)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Gets the owner of a process.
        /// 
        /// https://stackoverflow.com/questions/777548/how-do-i-determine-the-owner-of-a-process-in-c
        /// </summary>
        /// <param name="process">The process to inspect.</param>
        /// <returns>The name of the user, or null if it could not be read.</returns>
        public static string GetProcessUser(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                return wi.Name;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

    }//end class

    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            try
            {
                Process process = Process.GetProcessById(id);

                GetParentProcess(process.Handle);

                return GetParentProcess(process.Handle);
            }
            //Access was denied, or 
            catch 
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}
