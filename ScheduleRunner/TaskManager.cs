using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ScheduleRunner
{
    class TaskManager
    {
        public TaskManager(String method, String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer) {
            Helper.Banner();
            try
            {
                if (method.ToLower().Equals("create"))
                    CreateScheduledTask(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer);
                else if (method.ToLower().Equals("delete"))
                    RemoveScheduledTask(taskName, folder, remoteServer, null);
                else if (method.ToLower().Equals("run"))
                    RunScheduledTask(taskName, folder, remoteServer, null);
                else if (method.ToLower().Equals("query"))
                    ListScheduledTasks(taskName, folder, remoteServer);
                else if (method.ToLower().Equals("queryfolders"))
                    ListAllFolders(remoteServer);
                else if (method.ToLower().Equals("move"))
                    LateralMovement(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer);
                else
                    Console.WriteLine("[X] Error: Unknown method.");
            }
            catch (Exception)
            {
                Console.WriteLine("[X] Error: Unknown method.");
                return;
            }
        }

        TaskService CreateScheduledTask(String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer) {
            try
            {
                // Check key parameters
                if (taskName == null || trigger == null || program == null)
                {
                    Console.WriteLine("[X] Error: Missing parameters. \"/taskname, /program, /trigger\" must be defined. Please try again.");
                    return null;
                }

                TaskService ts;

                // Validate remote access
                if (remoteServer != null)
                {
                    ts = GetRemoteTaskService(remoteServer);
                    if (ts == null)
                        return null;
                    if (user == null)
                        user = "SYSTEM";
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
                    if(!folderIsExist)
                    {    
                        Console.WriteLine("[X] Error: The folder does not exist.");
                        return null;
                    }
                    taskName = folder + taskName;
                }

                TaskDefinition td = ts.NewTask();
                
                // Define trigger
                if (trigger.Equals("daily"))
                {
                    if (startTime == null)
                    {
                        Console.WriteLine("[X] Error: The modifier is not defined. Please try again. For example, \"/starttime:23:30\" to repeat the task daily at 11:30pm.");
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
                        Console.WriteLine("[X] Error: Wrong time format for \"/starttime:\". Please try again. For example, use \"/starttime:23:30\" to repeat the task daily at 11:30pm.");
                        return null;
                    }
                }
                else if (trigger.Equals("hourly"))
                {
                    TimeTrigger tt = new TimeTrigger();
                    if (modifier == null)
                    {
                        Console.WriteLine("[X] Error: The modifier is not defined. Please try again. For example, use \"/modifier:4\" to repeat the task every 4 hours.");
                        return null;
                    }
                    if (Int16.Parse(modifier) <= 23)
                    {
                        tt.Repetition.Interval = TimeSpan.FromHours(Int16.Parse(modifier));
                    }
                    else
                    {
                        Console.WriteLine("[X] Error: The modifier for hourly trigger should be lower than 24 hours. Please try again.");
                        return null;
                    }
                    td.Triggers.Add(tt);
                }
                else if (trigger.Equals("minute"))
                {
                    TimeTrigger tt = new TimeTrigger();
                    if (modifier == null) {
                        Console.WriteLine("[X] Error: The modifier is not defined. Please try again. For example, use \"/modifier:60\" to repeat the task every 60 minutes.");
                        return null;
                    }
                    if (Int16.Parse(modifier) <= 1439)
                    {
                        tt.Repetition.Interval = TimeSpan.FromMinutes(Int16.Parse(modifier));
                    }
                    else
                    {
                        Console.WriteLine("[X] Error: The modifier for minute trigger should be lower than 1439 minutes. Please try again.");
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
                    Console.WriteLine("[X] Error: No such schedule type. Please try again.");
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
                    return ts;
                }
                else
                {
                    Console.WriteLine("[X] Error: Unknown error. The scheduled task was not created.");
                    return null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[X] Error: You do not have sufficient permission to create the scheduled task.");
                return null;
            }
            catch (COMException)
            {
                Console.WriteLine("[X] Error: The user name could not be found.");
                return null;
            }
            catch (Exception)
            {
                Console.WriteLine("[X] Error: Error when creating the scheduled task. Please check your parameters again.");
                return null;
            }
        }

        void RemoveScheduledTask(String taskName, String folder, String remoteServer, TaskService ts)
        {
            // Check key parameters
            if (taskName == null)
            {
                Console.WriteLine("[X] Error: Missing parameter. \"/taskname\" must be defined. Please try again.");
                return;
            }

            if (ts == null) { 
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
                    Console.WriteLine("[X] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }

            // Check if the task exists
            if (CheckIfScheduledTaskExists(ts, taskName))
            {
                try
                {
                    // Start deleting the task
                    Console.WriteLine("[+] Deleting the scheduled task: " + taskName + "...");
                    ts.RootFolder.DeleteTask(taskName);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("[X] Error: Access is denied.");
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("[X] Error: Unable to delete the scheduled task.");
                    return;
                }

                if (!CheckIfScheduledTaskExists(ts, taskName))
                {
                    Console.WriteLine("[+] The scheduled task is deleted.");
                }
                else
                {
                    Console.WriteLine("[X] Error: The scheduled task cannot be deleted.");
                }
            }
            else
            {
                Console.WriteLine("[X] Error: The scheduled task does not exist.");
            }
        }

        void RunScheduledTask(String taskName, String folder, String remoteServer, TaskService ts)
        {
            // Check key paramters
            if (taskName == null)
            {
                Console.WriteLine("[X] Error: Missing parameter. \"/taskname\" must be defined. Please try again.");
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
                    Console.WriteLine("[X] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }
            Task t = ts.GetTask(taskName);

            // Execute the task if exists
            if (t == null)
            {
                Console.WriteLine("[X] Error: The scheduled task does not exist.");
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
                    Console.WriteLine("[X] Error: The scheduled task cannot be started.");
                    return;
                }
            }
        }

        void ListScheduledTasks(String taskName, String folder, String remoteServer)
        {
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
        
        void ListDetailsForScheduledTask(String taskName, String folder, String remoteServer)
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
                    Console.WriteLine("[X] Error: The folder does not exist.");
                    return;
                }
                taskName = folder + taskName;
            }

            if (!CheckIfScheduledTaskExists(ts, taskName))
            {
                Console.WriteLine("[X] Error: The scheduled task does not exist.");
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
        void ListScheduledTasksInFolder(TaskFolder fld, String remoteServer)
        {
            if (fld == null)
            {
                Console.WriteLine("[X] Error: This folder cannot be found.");
                return;
            }
            Console.WriteLine("[+] Folder: " + fld.ToString());
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
            Console.WriteLine("[+] Number of tasks: " + fld.Tasks.Count);
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

        void ListSubFolders(TaskFolder fld)
        {
            foreach (TaskFolder sfld in fld.SubFolders)
            {
                Console.WriteLine(sfld.Path);
                ListSubFolders(sfld);
            }
        }

        void LateralMovement(String taskName, String folder, String author, String description, String trigger, String program, String argument, String user, String modifier, String startTime, String remoteServer) {
            if (remoteServer == null || taskName == null || program == null)
            {
                Console.WriteLine("[X] Error: Missing parameters. \"/remoteserver, /taskname, /program\" must be defined. Please try again.");
                return;
            }
            if (trigger == null)
                trigger = "onlogon";
            TaskService ts = CreateScheduledTask(taskName, folder, author, description, trigger, program, argument, user, modifier, startTime, remoteServer);
            if (ts != null)
            {
                RunScheduledTask(taskName, folder, remoteServer, ts);
                RemoveScheduledTask(taskName, folder, remoteServer, ts);
            }
        }

        bool CheckIfScheduledTaskExists(TaskService ts, String taskName)
        {
            Task t = ts.GetTask(taskName);
            if (t == null)
                return false;
            else
                return true;
        }

        bool CheckIfFolderExists(TaskService ts, String folder)
        {
            TaskFolder tf = ts.GetFolder(folder);
            if (tf == null)
                return false;
            else
                return true;
        }

        TaskService GetRemoteTaskService(String remoteServer)
        {
            try
            {
                Console.WriteLine("[+] Connecting to target server: " + remoteServer + "...");
                TaskService ts = new TaskService(remoteServer);
                Console.WriteLine("[+] Connected to " + remoteServer + ".");
                return ts;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("[X] Error: The network path was not found.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[X] Error: Access is denied.");
                return null;
            }
            catch (Exception)
            {
                Console.WriteLine("[X] Error: Unknown error while accessing remote server.");
                return null;
            }
        }
    }
}
