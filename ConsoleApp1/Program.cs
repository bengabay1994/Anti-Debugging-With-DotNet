using AntiDebugDotNet.Enums;
using AntiDebugDotNet.Structs;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;
using Vanara.PInvoke;
using AntiDebugDotNet.Wrappers;
using System.Text;

namespace AntiDebugDotNet
{
    [SupportedOSPlatform("windows")]
    internal unsafe partial class Program
    {
        [LibraryImport("user32.dll")]
        private static partial nint GetShellWindow();

        [LibraryImport("user32.dll")]
        private static partial uint GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

        [LibraryImport("ntdll.dll")]
        private static partial uint NtQuerySystemInformation(SystemInformationClass systemInformationClass, nint SystemInformation, uint systemInformationLength, out uint returnLength);

        [LibraryImport("ntdll.dll")]
        private static partial uint NtQueryObject(nint threadHandle, ObjectInformationClass objectInformationClass, nint objectInformation, uint objectInformationLength, out uint returnLength);

        [LibraryImport("ntdll.dll")]
        private static partial uint NtSetInformationThread(nint hThread, ThreadInfoClass threadInfoClass, nint ThreadInformation, uint processInformationLength);

        [LibraryImport("ntdll.dll")]
        private static partial uint NtOpenThread(out nint threadHandle, ThreadAccessMask desiredAccess, ref ObjectAttributes objectAttributes, ref ClientId clientId);

        [LibraryImport("ntdll.dll")]
        private static partial uint NtCreateThreadEx(out nint hThread,
            ThreadAccessMask desiredAccess,
            nint objectAttributes,
            nint hProcess,
            nint startRoutine,
            nint arguments,
            uint createFlags,
            nuint zeroBits,
            nuint stackSize,
            nuint maximumStackSize,
            nint attributeList);

        private const string c_debuggerDetectedStr = "!Debugger Detected!";

        private const string c_noDebuggerFoundStr = "no debugger was found.";

        private static uint s_currentProcessId = (uint)Process.GetCurrentProcess().Id;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to AntiDebugDotNet. A program that uses different anti-debugging techniques that are explained at https://medium.com/@bengabay1994/anti-debugging-with-net-in-windows-environment-d5955e207c86.\nTo activate the 'hiding all threads' from the debugger. Execute this program passing at least 1 argument.\ni.e \".\\AntiDebugDotNet.exe arg\"");
            PressEnterToContinue("If you like to attach a debugger to this program, now its the time to do it.");
            AskingThePeb();
            if (args.Length > 0)
            {
                HideAllThreads();
            }
            CreateHiddenThread();
            ShowDebugPort();
            ShowDebugObject();
            ProcessHandleTracing();
            ParentIsExplorer();
            OpeningOurOwnImage();
            OpeningAnyLoadedLibrary();
            KernelDebugger();
            KernelDebuggerFromKUserSharedData();
            PressEnterToContinue("Press enter to exit the program...", false);
        }

        private static void PrintSeparator(String sectionName)
        {
            Console.WriteLine($"==========================={sectionName}===========================");
        }

        private static void AskingThePeb()
        {
            PrintSeparator("PEB");
            HPROCESS hProcess = Kernel32.GetCurrentProcess();
            if (hProcess.IsNull)
            {
                Console.WriteLine("Failed to get current process handle");
                return;
            }


            var basicInfo = NtDll.NtQueryInformationProcess<NtDll.PROCESS_BASIC_INFORMATION>(
                                hProcess,
                                NtDll.PROCESSINFOCLASS.ProcessBasicInformation);
            if (!basicInfo.HasValue)
            {
                Console.WriteLine("Failed to get process basic information");
                return;
            }
            // Not using Vanara.PInvoke.NtDll.PEB due to lack of some fields.
            Peb pebInstance = Marshal.PtrToStructure<Peb>(basicInfo.Value.PebBaseAddress);

            // Heap Section
            Kernel32.HHEAP hHeap = Kernel32.GetProcessHeap();
            if (hHeap.IsNull)
            {
                Console.WriteLine("Failed to get process heap handle");
                return;
            }
            ProcessHeap processHeap = Marshal.PtrToStructure<ProcessHeap>((IntPtr)hHeap);

            // StartupInfo Section
            Kernel32.GetStartupInfo(out Kernel32.STARTUPINFO startupInfo);

            Console.WriteLine($"BeingDebugged: {pebInstance.BeingDebugged}. {(pebInstance.BeingDebugged == 0 ? c_noDebuggerFoundStr : c_debuggerDetectedStr)}");
            Console.WriteLine($"NtGlobalFlag: 0x{pebInstance.NtGlobalFlag.ToString("X")}. {(pebInstance.NtGlobalFlag == 0 ? c_noDebuggerFoundStr : c_debuggerDetectedStr)}");
            Console.WriteLine($"PEB->ProcessHeap->FLags: 0x{processHeap.Flags.ToString("X")}. {(processHeap.Flags == 2 ? c_noDebuggerFoundStr : c_debuggerDetectedStr)}");
            Console.WriteLine($"PEB->Heap->ForceFLags: 0x{processHeap.ForceFlags.ToString("X")}. {(processHeap.ForceFlags == 0 ? c_noDebuggerFoundStr : c_debuggerDetectedStr)}");
            Console.WriteLine($"StartupInfo->lpDestkop: {startupInfo.lpDesktop}. {(string.IsNullOrEmpty(startupInfo.lpDesktop) ? c_debuggerDetectedStr : c_noDebuggerFoundStr)}");
            PrintSeparator("PEB");
        }

        private static void HideAllThreads()
        {
            PrintSeparator("Hiding Threads");
            PressEnterToContinue("Hiding all threads from the debugger.");
            Process myProcess = Process.GetCurrentProcess();
            Console.WriteLine($"Number of threads in the process: {myProcess.Threads.Count}");
            foreach (ProcessThread thread in myProcess.Threads)
            {
                ClientId clientId = GetClientId(thread.Id);
                ObjectAttributes objectAttributes = new ObjectAttributes { Length = (uint)sizeof(ObjectAttributes) };
                NTStatus status = new NTStatus(NtOpenThread(out nint hThread, ThreadAccessMask.THREAD_SET_INFORMATION, ref objectAttributes, ref clientId));
                if (status.Failed)
                {
                    Console.WriteLine($"Failed to open a handle to thread id: {thread.Id}. Error status code: {status}");
                    continue;
                }
                Console.WriteLine($"Hiding Thread Id: {thread.Id}");
                status = new NTStatus(NtSetInformationThread(hThread, ThreadInfoClass.ThreadHideFromDebugger, 0, 0));
                if (status.Failed)
                {
                    Console.WriteLine($"Failed to hide thread id: {thread.Id}. Error status code: {status}");
                }
                Kernel32.CloseHandle(hThread);
            }

            PrintSeparator("Hiding Threads");
        }

        private unsafe static void HideCurrentThread()
        {
            uint currentThreadId = Kernel32.GetCurrentThreadId();
            ClientId clientId = new ClientId { UniqueProcess = Process.GetCurrentProcess().Id, UniqueThread = (nint)currentThreadId };
            ObjectAttributes objectAttributes = new ObjectAttributes { Length = (uint)sizeof(ObjectAttributes) };
            nint hThread = IntPtr.Zero;
            NTStatus status = new NTStatus(NtOpenThread(out hThread, ThreadAccessMask.THREAD_SET_INFORMATION, ref objectAttributes, ref clientId));
            if (status.Failed)
            {
                Console.WriteLine("Failed to open a handle to the current thread.");
                return;
            }
            status = new NTStatus(NtSetInformationThread(hThread, ThreadInfoClass.ThreadHideFromDebugger, 0, 0));
            if (status.Failed)
            {
                Console.WriteLine("Failed to hide the current thread.");
                Kernel32.CloseHandle(hThread);
                return;
            }
            Kernel32.CloseHandle(hThread);
        }

        private static void CreateHiddenThread()
        {
            PrintSeparator("Create a Hidden Thread");
            nint hThread = nint.Zero;
            nint hProcess = Process.GetCurrentProcess().Handle;
            nint startRoutine = Marshal.GetFunctionPointerForDelegate(doNothing);
            uint THREAD_CREATE_FLAGS_HIDE_FROM_DEBUGGER = 4;
            Console.WriteLine("Creating a hidden thread that calls a function that does nothing but print 1 sentence.");
            NTStatus status = new NTStatus(NtCreateThreadEx(out hThread,
                ThreadAccessMask.THREAD_ALL_ACCESS,
                nint.Zero,
                hProcess,
                startRoutine,
                nint.Zero,
                THREAD_CREATE_FLAGS_HIDE_FROM_DEBUGGER,
                0,
                0,
                0,
                nint.Zero));
            if (status.Failed)
            {
                Console.WriteLine("Failed to create a hidden thread.");
            }
            if (hThread != nint.Zero)
            {
                Kernel32.CloseHandle(hThread);
            }
            Thread.Sleep(500);
            PrintSeparator("Create a Hidden Thread");
        }

        private static void ShowDebugObject()
        {
            PrintSeparator("Debug Object");
            Console.WriteLine("Looking for a debug object handle to the current process");
            HPROCESS hProcess = Kernel32.GetCurrentProcess();
            NtDll.NtQueryResult<nint>? debugObjectHandle = null;
            try
            {
                debugObjectHandle = NtDll.NtQueryInformationProcess<nint>(hProcess, NtDll.PROCESSINFOCLASS.ProcessDebugObjectHandle);
                if (!debugObjectHandle.HasValue)
                {
                    Console.WriteLine("Failed to get the debug object handle.");
                }
                else
                {
                    Console.WriteLine($"Debug Object Handle: {debugObjectHandle?.Value}");
                }
            }
            catch (System.ComponentModel.Win32Exception exc)
            {
                Console.WriteLine("Debug Port is 0.");
            }

            Console.WriteLine("Looking for ANY debug object!");
            DetectDebuggingSessions();

            PrintSeparator("Debug Object");
        }

        private static void DetectDebuggingSessions()
        {
            uint size = 20000; // Start with 20KB space for the array.
            nint baseAddr = Marshal.AllocHGlobal((nint)size);

            while (new NTStatus(NtQueryObject(0, ObjectInformationClass.ObjectAllTypesInformation, baseAddr, size, out uint returnLength)).Equals(NTStatus.STATUS_INFO_LENGTH_MISMATCH))
            {
                size *= 2;
                baseAddr = Marshal.ReAllocHGlobal(baseAddr, (nint)size);
            }
            ObjectAllInformationWrapper objectAllInformation = new ObjectAllInformationWrapper(baseAddr);
            foreach (ObjectTypeInformation objType in objectAllInformation.ObjectTypeInformation)
            {
                if (objType.TypeName.Buffer != nint.Zero && objType.TypeName.ToString().Equals("DebugObject", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"There are {objType.TotalNumberOfObjects} Objects of type DebugObjects found on the system");
                }
            }
            Marshal.FreeHGlobal(baseAddr);
        }

        private static void ProcessHandleTracing()
        {
            PrintSeparator("Handle Tracing");
            try
            {
                Kernel32.CloseHandle((nint)0xBADD);
                Console.WriteLine($"Handle Tracing is not enabled. {c_noDebuggerFoundStr}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Handle Tracing is enabled. {c_debuggerDetectedStr}");
            }
            PrintSeparator("Handle Tracing");
        }

        private static void ParentIsExplorer()
        {
            PrintSeparator("Who Created Us?");
            Console.WriteLine("Checking if explorer.exe is our parent process");
            // get a handle to explorer.exe process
            nint explorerHandle = GetShellWindow();
            int explorerProcId = -1;
            // get the PID of explorer.exe
            GetWindowThreadProcessId(explorerHandle, out explorerProcId);
            if (explorerProcId == -1)
            {
                Console.WriteLine("Failed to get the PID of explorer.exe");
                return;
            }
            HPROCESS hProc = Kernel32.GetCurrentProcess();
            if (hProc.IsNull)
            {
                Console.WriteLine("Failed to get the current process handle");
                return;
            }
            var procInfo = NtDll.NtQueryInformationProcess<NtDll.PROCESS_BASIC_INFORMATION>(hProc, NtDll.PROCESSINFOCLASS.ProcessBasicInformation);
            if (!procInfo.HasValue)
            {
                Console.WriteLine("Failed to get the process basic information of the current process");
                return;
            }
            if (procInfo.Value.InheritedFromUniqueProcessId == (nuint)explorerProcId)
            {
                Console.WriteLine("Our parent process is explorer.exe");
            }
            else
            {
                Console.WriteLine($"Our parent process is not explorer.exe!. Parent process PID: {procInfo.Value.InheritedFromUniqueProcessId}");
            }
            PrintSeparator("Who Created Us?");
        }

        private static void OpeningOurOwnImage()
        {
            PrintSeparator("Opening a Handle to our own image file.");
            StringBuilder sb = new StringBuilder(Kernel32.MAX_PATH);
            Kernel32.GetModuleFileName(0, sb, Kernel32.MAX_PATH);
            Console.WriteLine($"Process name: {sb}");
            var handle = Kernel32.CreateFile(sb.ToString(), Kernel32.FileAccess.GENERIC_READ, 0, null, Kernel32.CreationOption.OPEN_EXISTING, 0, 0);
            if (handle.IsInvalid)
            {
                Console.WriteLine($"Failed to open a handle to our own image file. Meaning {c_debuggerDetectedStr}");
            }
            else
            {
                Console.WriteLine($"Successfully opened a handle to our own image file. Meaning {c_noDebuggerFoundStr}");
            }
            PrintSeparator("Opening a Handle to our own image file.");
        }

        private static void OpeningAnyLoadedLibrary()
        {
            PrintSeparator("Opening a Handle to a loaded library");
            Console.WriteLine("First we are loading calc.exe to the current process");
            StringBuilder sb = new StringBuilder("C:\\Windows\\System32\\calc.exe");
            Kernel32.SafeHINSTANCE safeHinstance = Kernel32.LoadLibrary(sb.ToString());
            if(safeHinstance.IsInvalid)
            {
                Console.WriteLine("Failed to load calc.exe to the current process.");
                PrintSeparator("Opening a Handle to a loaded library");
                return;
            }
            Console.WriteLine("Successfully loaded calc.exe to the current process.");
            Kernel32.SafeHFILE handle = Kernel32.CreateFile(sb.ToString(), Kernel32.FileAccess.GENERIC_READ, 0, null, Kernel32.CreationOption.OPEN_EXISTING, 0, 0);
            if (handle.IsInvalid)
            {
                Console.WriteLine($"Failed to open a handle to calc.exe. Meaning {c_debuggerDetectedStr}");
            }
            else
            {
                Console.WriteLine($"Successfully opened a handle to calc.exe. Meaning {c_noDebuggerFoundStr}");
            }
            PrintSeparator("Opening a Handle to a loaded library");
        }

        private static void KernelDebugger() {
            PrintSeparator("Kernel Debugger");
            SystemKernelDebuggerInformation systemKernelDebuggerInformation = new SystemKernelDebuggerInformation();
            NTStatus status = new NTStatus(
            NtQuerySystemInformation(SystemInformationClass.SystemKernelDebuggerInformation,
                                    (nint)(&systemKernelDebuggerInformation),
                                    2,
                                    out uint returnLength));

            if (status.Failed)
            {
                Console.WriteLine($"Failed to get the kernel debugger information. Error: {status}");
                return;
            }
            Console.WriteLine($"Kernel Debugger Enabled Field: {systemKernelDebuggerInformation.KernelDebuggerEnabled}");
            PrintSeparator("Kernel Debugger");
        }

        private static void KernelDebuggerFromKUserSharedData() {
            PrintSeparator("KUSER_SHARED_DATA");
            nint KdDebuggerEnabledAddress = new nint(0x7ffe0000 + 0x2d4);
            byte KdDebuggerEnabled = Marshal.ReadByte(KdDebuggerEnabledAddress);
            Console.WriteLine($"Kernel Debugger Enabled Field: {KdDebuggerEnabled}");
            PrintSeparator("KUSER_SHARED_DATA");
        }

        private static void ShowDebugPort()
        {
            PrintSeparator("Debug Port");
            HPROCESS hProcess = Kernel32.GetCurrentProcess();
            var debugPort = NtDll.NtQueryInformationProcess<nint>(hProcess, NtDll.PROCESSINFOCLASS.ProcessDebugPort);
            if (!debugPort.HasValue)
            {
                Console.WriteLine("Failed to get the debug port.");
            }
            else
            {
                Console.WriteLine($"Debug Port: {debugPort.Value}. {(debugPort.Value == 0 ? c_noDebuggerFoundStr : c_debuggerDetectedStr)}");
            }
            PrintSeparator("Debug Port");
        }

        private static void PressEnterToContinue(string? message = null, bool printEnter = true)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }
            if(printEnter == true)
            {
                Console.WriteLine("Press Enter To Continue...");
            }
            Console.ReadLine();
        }

        private static ClientId GetClientId(nint tid = -1)
        {
            if (tid == -1)
            {
                tid = (nint)Kernel32.GetCurrentThreadId();
            }
            return new ClientId { UniqueProcess = (nint)s_currentProcessId, UniqueThread = tid };
        }

        private static void doNothing()
        {
            Console.WriteLine("Doing nothing (probably called by a hidden thread. Try to place a breakpoint on me and see. :) )");
        }
    }
}
