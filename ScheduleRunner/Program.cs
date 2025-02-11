using System;
using System.Collections.Generic;

namespace ScheduleRunner
{
    class Program
    {
        private static string method;
        private static string taskName;
        private static string folder;
        private static string workingDirectory;
        private static string author;
        private static string description;
        private static string trigger;
        private static string program;
        private static string argument;
        private static string user;
        private static string modifier;
        private static string startTime;
        private static string remoteServer;
        private static bool hide;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                // Display help if requested
                if (args[0] == "/help" || args[0] == "-h" || args[0] == "/h" || args[0] == "help")
                {
                    Helper.PrintHelp();
                    return;
                }

                // Parse arguments into a dictionary
                Dictionary<string, string> argsParam = Helper.ParseArgs(args);
                if (argsParam == null)
                    return;

                // Map the parsed arguments to corresponding variables
                method = argsParam.GetValueOrDefault("method");
                taskName = argsParam.GetValueOrDefault("taskname");
                folder = argsParam.GetValueOrDefault("folder");
                workingDirectory = argsParam.GetValueOrDefault("workingdir");
                author = argsParam.GetValueOrDefault("author");
                description = argsParam.GetValueOrDefault("description");
                trigger = argsParam.GetValueOrDefault("trigger");
                program = argsParam.GetValueOrDefault("program");
                argument = argsParam.GetValueOrDefault("argument");
                user = argsParam.GetValueOrDefault("user");
                modifier = argsParam.GetValueOrDefault("modifier");
                startTime = argsParam.GetValueOrDefault("starttime");
                remoteServer = argsParam.GetValueOrDefault("remoteserver");

                // Handle the "technique" argument for hide flag
                if (argsParam.ContainsKey("technique"))
                {
                    string technique = argsParam["technique"];
                    if (technique.Contains("hide"))
                        hide = true;
                }

                // Create a new TaskManager instance
                new TaskManager(method, taskName, folder, workingDirectory, author, description, trigger, program, argument, user, modifier, startTime, remoteServer, hide);
            }
            else
            {
                // If no arguments are passed, display help
                Helper.PrintHelp();
            }
        }
    }
}
