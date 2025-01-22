using System;
using System.Collections.Generic;

namespace ScheduleRunner
{
    public class Helper
    {
        public static Dictionary<string, string> ParseArgs(string[] args) {
            try
            {
                Dictionary<string, string> ret = new Dictionary<string, string>();
                for (int i = 0; i < args.Length; i++)
                    ret.Add(args[i].Split(':')[0].Remove(0, 1).ToLower(), args[i].Split(new[] { ':' }, 2)[1]);
                return ret;
            }
            catch (Exception)
            {
                Console.WriteLine("[X] Your command is wrong. Please check help page.");
                return null;
            }
        }

        public static void Banner() {
            Console.WriteLine(@"");
            Console.WriteLine(@"=================================================================================");
            Console.WriteLine(@"|    _____      __             __      __     ____                              |");
            Console.WriteLine(@"|   / ___/_____/ /_  ___  ____/ /_  __/ /__  / __ \__  ______  ____  ___  _____ |");
            Console.WriteLine(@"|   \__ \/ ___/ __ \/ _ \/ __  / / / / / _ \/ /_/ / / / / __ \/ __ \/ _ \/ ___/ |");
            Console.WriteLine(@"|  ___/ / /__/ / / /  __/ /_/ / /_/ / /  __/ _, _/ /_/ / / / / / / /  __/ /     |");
            Console.WriteLine(@"| /____/\___/_/ /_/\___/\__,_/\__,_/_/\___/_/ |_|\__,_/_/ /_/_/ /_/\___/_/      |");
            Console.WriteLine(@"|                                                                               |");
            Console.WriteLine(@"| Version: 1.3                                                                  |");
            Console.WriteLine(@"|  Author: Chris Au                                                             |");
            Console.WriteLine(@"| Twitter: @netero_1010                                                         |");
            Console.WriteLine(@"|  Github: @netero1010                                                          |");
            Console.WriteLine(@"=================================================================================");
            Console.WriteLine(@"");
        }

        public static void PrintHelp() {
            Helper.Banner();
            Console.WriteLine(@"Methods (/method):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    create        - Create a new scheduled task");
            Console.WriteLine(@"    delete        - Delete an existing scheduled task");
            Console.WriteLine(@"    run           - Execute an existing scheduled task");
            Console.WriteLine(@"    query         - Query details for a scheduled task or all scheduled tasks under a folder");
            Console.WriteLine(@"    queryfolders  - Query all sub-folders in scheduled task recursively");
            Console.WriteLine(@"    move          - Perform lateral movement using scheduled task (automatically create, run and delete)");
            Console.WriteLine(@"");
            Console.WriteLine(@"[*] are mandatory fields.");
            Console.WriteLine(@"");
            Console.WriteLine(@"Options for scheduled task creation (/method:create):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    [*] /taskname     - Specify the name of the scheduled task");
            Console.WriteLine(@"    [*] /program      - Specify the program that the task runs");
            Console.WriteLine(@"    [*] /trigger      - Specify the schedule type. The valid values include: ""minute"", ""hourly"", ""daily"", ""weekly"", ""onstart"", ""onlogon"", and ""onidle""");
            Console.WriteLine(@"    /modifier         - Specify how often the task runs within its schedule type. Applicable only for schedule type such as ""minute"" (e.g., 1-1439 minutes), ""hourly"" (e.g., 1-23 hours) and ""weekly"" (e.g., mon,sat,sun)");
            Console.WriteLine(@"    /starttime        - Specify the start time for daily schedule type (e.g., 23:30)");
            Console.WriteLine(@"    /argument         - Specify the command line argument for the program");
            Console.WriteLine(@"    /folder           - Specify the folder where the scheduled task stores (default: \)");
            Console.WriteLine(@"    /workingdir       - Specify the working directory in which the scheduled task will be executed");
            Console.WriteLine(@"    /author           - Specify the author of the scheduled task");
            Console.WriteLine(@"    /description      - Specify the description for the scheduled task");
            Console.WriteLine(@"    /remoteserver     - Specify the hostname or IP address of a remote computer");
            Console.WriteLine(@"    /user             - Run the task with a specified user account");
            Console.WriteLine(@"    /technique        - Specify evasion technique:");
            Console.WriteLine(@"                        ""hide"": A technique used by HAFNIUM malware that will hide the scheduled task from ""/method:query"", ""schtasks /query"", and Task Scheduler");
            Console.WriteLine(@"                        (This technique does not support remote execution due to privilege of remote registry. It requires ""NT AUTHORITY\SYSTEM"" and the task will continue to run until system reboot even after task deletion)");
            Console.WriteLine(@"");
            Console.WriteLine(@"Options for scheduled task deletion (/method:delete):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    [*] /taskname     - Specify the name of the scheduled task");
            Console.WriteLine(@"    /folder           - Specify the folder where the scheduled task stores");
            Console.WriteLine(@"    /remoteserver     - Specify the hostname or IP address of a remote computer");
            Console.WriteLine(@"    /technique        - Specify when the scheduled task was created using evasion technique:");
            Console.WriteLine(@"                        ""hide"": Delete scheduled task that used ""hiding scheduled task"" technique");
            Console.WriteLine(@"                        (The deletion requires ""NT AUTHORITY\SYSTEM"" and the task will continue to run until system reboot even after task deletion)");
            Console.WriteLine(@"");
            Console.WriteLine(@"Options for scheduled task execution (/method:run):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    [*] /taskname     - Specify the name of the scheduled task");
            Console.WriteLine(@"    /folder           - Specify the folder where the scheduled task stores");
            Console.WriteLine(@"    /remoteserver     - Specify the hostname or IP address of a remote computer");
            Console.WriteLine(@"");
            Console.WriteLine(@"Options for scheduled task query (/method:query):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    /taskname         - Specify the name of the scheduled task");
            Console.WriteLine(@"    /folder           - Specify the folder where the scheduled task stores");
            Console.WriteLine(@"    /remoteserver     - Specify the hostname or IP address of a remote computer");
            Console.WriteLine(@"");
            Console.WriteLine(@"Options for scheduled task lateral movement (/method:move):");
            Console.WriteLine(@"");
            Console.WriteLine(@"    [*] /taskname     - Specify the name of the scheduled task");
            Console.WriteLine(@"    [*] /program      - Specify the program that the task runs");
            Console.WriteLine(@"    [*] /remoteserver - Specify the hostname or IP address of a remote computer");
            Console.WriteLine(@"    /trigger          - Specify the schedule type. The valid values include: ""minute"", ""hourly"", ""daily"", ""weekly"", ""onstart"", ""onlogon"", and ""onidle""");
            Console.WriteLine(@"    /modifier         - Specify how often the task runs within its schedule type. Applicable only for schedule type such as ""minute"" (e.g., 1-1439 minutes), ""hourly"" (e.g., 1-23 hours) and ""weekly"" (e.g., mon,sat,sun)");
            Console.WriteLine(@"    /starttime        - Specify the start time for daily schedule type (e.g., 23:30)");
            Console.WriteLine(@"    /argument         - Specify the command line argument for the program");
            Console.WriteLine(@"    /folder           - Specify the folder where the scheduled task stores (default: \)");
            Console.WriteLine(@"    /workingdir       - Specify the working directory in which the scheduled task will be executed");
            Console.WriteLine(@"    /author           - Specify the author of the scheduled task");
            Console.WriteLine(@"    /description      - Specify the description for the scheduled task");
            Console.WriteLine(@"    /user             - Run the task with a specified user account");
            Console.WriteLine(@"");
            Console.WriteLine(@"Example:");
            Console.WriteLine(@"");
            Console.WriteLine(@"Create a scheduled task called ""Cleanup"" that will be executed every day at 11:30 p.m.:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:create /taskname:Cleanup /trigger:daily /starttime:23:30 /program:calc.exe /description:""Some description"" /author:netero1010");
            Console.WriteLine(@"");
            Console.WriteLine(@"Create a scheduled task called ""Cleanup"" that will be executed every 4 hours on a remote server:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:create /taskname:Cleanup /trigger:hourly /modifier:4 /program:rundll32.exe /argument:c:\temp\payload.dll /remoteserver:TARGET-PC01");
            Console.WriteLine(@"");
            Console.WriteLine(@"Delete a scheduled task called ""Cleanup"":");
            Console.WriteLine(@"    ScheduleRunner.exe /method:delete /taskname:Cleanup");
            Console.WriteLine(@"");
            Console.WriteLine(@"Execute a scheduled task called ""Cleanup"":");
            Console.WriteLine(@"    ScheduleRunner.exe /method:run /taskname:Cleanup");
            Console.WriteLine(@"");
            Console.WriteLine(@"Query details for a scheduled task called ""Cleanup"" under ""\Microsoft\Windows\CertificateServicesClient"" folder on a remote server:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:query /taskname:Cleanup /folder:\Microsoft\Windows\CertificateServicesClient /remoteserver:TARGET-PC01");
            Console.WriteLine(@"");
            Console.WriteLine(@"Query all scheduled tasks under a specific folder ""\Microsoft\Windows\CertificateServicesClient"" on a remote server:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:query /folder:\Microsoft\Windows\CertificateServicesClient /remoteserver:TARGET-PC01");
            Console.WriteLine(@"");
            Console.WriteLine(@"Query all sub-folders in scheduled task:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:queryfolders");
            Console.WriteLine(@"");
            Console.WriteLine(@"Perform lateral movement using scheduled task to a remote server using a specific user account:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:move /taskname:Demo /remoteserver:TARGET-PC01 /program:rundll32.exe /argument:c:\temp\payload.dll /user:netero1010");
            Console.WriteLine(@"");
            Console.WriteLine(@"Technique - hide:");
            Console.WriteLine(@"");
            Console.WriteLine(@"Create a scheduled task called ""Cleanup"" using hiding scheduled task technique:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:create /taskname:Cleanup /trigger:daily /starttime:23:30 /program:calc.exe /description:""Some description"" /author:netero1010 /technique:hide");
            Console.WriteLine(@"");
            Console.WriteLine(@"Delete a scheduled task called ""Cleanup"" that used hiding scheduled task technique:");
            Console.WriteLine(@"    ScheduleRunner.exe /method:delete /taskname:Cleanup /technique:hide");
        }
    }
}
