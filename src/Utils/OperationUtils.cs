﻿using System;

namespace Win10BloatRemover.Utils
{
    static class OperationUtils
    {
        /**
         *  Removes the specified component using install-wim-tweak synchronously
         *  Messages from install-wim-tweak process are printed asynchronously (as soon as they are written to stdout/stderr)
         */
        public static void RemoveComponentUsingInstallWimTweakIfAllowed(string component)
        {
            if (!Configuration.Instance.AllowInstallWimTweak)
            {
                ConsoleUtils.WriteLine($"Skipped removal of component {component} using install-wim-tweak since " +
                                       @"option ""AllowInstallWimTweak"" is set to false.", ConsoleColor.DarkYellow);
                return;
            }

            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            int installWimTweakExitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(Program.InstallWimTweakPath, $"/o /c {component} /r");
            if (installWimTweakExitCode == SystemUtils.EXIT_CODE_SUCCESS)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
                ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: " +
                                        "install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
        }
    }
}
