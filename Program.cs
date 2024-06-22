using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using DiskAccessLibrary;
using DiskAccessLibrary.LogicalDiskManager;
using DiskAccessLibrary.Win32;

namespace USBUtils
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // get usb disk and call PhysicalDisk#SetOnlineStatus(false)
            // Get-CimClass -Namespace "root/Microsoft/Windows/Storage" -ClassName MSFT_PhysicalDisk

            // PhysicalDisk disk = new PhysicalDisk(0);
            // disk.SetOnlineStatus(false);
            // Console.WriteLine(disk.GetOnlineStatus());
            if (!IsCurrentProcessElevated())
            {
                // Console.WriteLine("Please run this program as an administrator.");
                Task task = StartElevatedAsync(args, CancellationToken.None);
                task.Wait();
                return;
            }
            List<PhysicalDisk> physicalDisks = PhysicalDiskHelper.GetPhysicalDisks();
            if (physicalDisks.Count == 0)
            {
                Console.WriteLine("No physical disks found.");
                return;
            }
            int diskNumber;
            bool userInput = args.Length == 0;
            if (userInput)
            {
                try
                {
                    for (int i = 0; i < physicalDisks.Count; i++)
                    {
                        Console.WriteLine($"Physical Disk #{i}: {physicalDisks[i].Description}");
                    }
                    Console.WriteLine("Select a disk number:");
                    diskNumber = int.Parse(Console.ReadLine());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
            else
            {
                if (!int.TryParse(args[0], out diskNumber) || diskNumber < 0 || diskNumber >= physicalDisks.Count)
                {
                    Console.WriteLine("Invalid input.");
                    return;
                }
            }

            PhysicalDisk disk = physicalDisks[diskNumber];
            Console.WriteLine("Selected disk: " + disk.Description);
            disk.SetOnlineStatus(!disk.GetOnlineStatus());
            Console.WriteLine("Online status is now: " + disk.GetOnlineStatus());
            if (userInput)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool IsCurrentProcessElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://github.com/dotnet/sdk/blob/v6.0.100/src/Cli/dotnet/Installer/Windows/WindowsUtils.cs#L38
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            // https://github.com/dotnet/maintenance-packages/blob/62823150914410d43a3fd9de246d882f2a21d5ef/src/Common/tests/TestUtilities/System/PlatformDetection.Unix.cs#L58
            // 0 is the ID of the root user
            return geteuid() == 0;
        }
        public static async Task StartElevatedAsync(string[] args, CancellationToken cancellationToken)
        {
            var currentProcessPath = Environment.ProcessPath ?? Path.ChangeExtension(typeof(Program).Assembly.Location, "exe");
            var processStartInfo = CreateProcessStartInfo(currentProcessPath, args);

            using var process = Process.Start(processStartInfo)
                ?? throw new InvalidOperationException("Could not start process.");

            await process.WaitForExitAsync(cancellationToken);
        }

        private static ProcessStartInfo CreateProcessStartInfo(string processPath, string[] args)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
            };
            ConfigureProcessStartInfoForWindows(startInfo, processPath, args);
            return startInfo;
        }

        private static void ConfigureProcessStartInfoForWindows(ProcessStartInfo startInfo, string processPath, string[] args)
        {
            startInfo.Verb = "runas";
            startInfo.FileName = processPath;

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }
    }
}