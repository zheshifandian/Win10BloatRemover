﻿using System;
using System.IO;
using System.Management.Automation;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    /**
     *  ServiceRemover
     *  Performs backup (export of registry keys) and removal of the services passed into the constructor
     */
    class ServiceRemover
    {
        private readonly string[] servicesToRemove;
        private bool backupPerformed,
                     removalPerformed;

        public ServiceRemover(string[] servicesToRemove)
        {
            this.servicesToRemove = servicesToRemove;
        }

        public ServiceRemover PerformBackup()
        {
            if (backupPerformed)
                throw new InvalidOperationException("Backup already done!");

            DirectoryInfo backupDirectory = Directory.CreateDirectory($"./servicesBackup_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}");
            foreach (string serviceName in servicesToRemove)
            {
                // Here we find all the services that start with the specified service name, in order to include services that end with a random code
                // Destination file will have the name of the service (%~nx assumes that the string is a path and returnes only filename + extension)
                // TODO REWRITE USING POWERSHELL
                SystemUtils.ExecuteWindowsCommand($"for /f \"tokens=1\" %I in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\" /k /f \"{serviceName}\" ^| find /i \"{serviceName}\"') " +
                                                  $"do reg export %I {backupDirectory.FullName}\\%~nxI.reg");
            }

            backupPerformed = true;
            return this;    // allows chaining with PerformRemoval
        }

        public void PerformRemoval()
        {
            if (!backupPerformed)
                throw new InvalidOperationException("Backup services before removing them!");
            if (removalPerformed)
                throw new InvalidOperationException("Removal already performed.");

            foreach (string serviceName in servicesToRemove)
            {
                using (PowerShell psInstance = PowerShell.Create())
                {
                    string removalScript = "$services = Get-ChildItem -Path HKLM:\\SYSTEM\\CurrentControlSet\\Services -Name |" +
                                                        "Where-Object {$_ -Match \"^" + serviceName + "\"};" +
                                           "if ($services) {" +
                                               "$services | ForEach-Object {" +
                                                    "sc.exe delete $_;" +
                                                    "if ($LASTEXITCODE = 5) {" +
                                                        "Write-Host \"Access denied\"" +    // TODO Set-Acl
                                                    "}" +
                                               "}" +
                                           "} else { Write-Host \"Service " + serviceName + " not found\" }";

                    Console.WriteLine($"\nRemoving {serviceName} service...");
                    psInstance.RunScriptAndPrintOutput(removalScript);
                }
            }

            PerformAdditionalTasks();
            removalPerformed = true;
        }

        /**
         *  Additional tasks to disable telemetry-related features
         *  Include blocking of CompatTelRunner, Inventory (collection of installed programs), Steps Recorder, Compatibility Assistant
         */
        private void PerformAdditionalTasks()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener"))
                key.SetValue("Start", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
            {
                key.SetValue("AITEnable", 0, RegistryValueKind.DWord);
                key.SetValue("DisableInventory", 1, RegistryValueKind.DWord);
                key.SetValue("DisablePCA", 1, RegistryValueKind.DWord);
                key.SetValue("DisableUAR", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                key.SetValue("EnableSmartScreen", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
        }
    }
}