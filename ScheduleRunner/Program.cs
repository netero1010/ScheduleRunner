using System.Collections.Generic;

namespace ScheduleRunner
{
    class Program
    {
        private static string method = null;
        private static string taskName = null;
        private static string folder = null;
        private static string author = null;
        private static string description = null;
        private static string trigger = null;
        private static string program = null;
        private static string agrument = null;
        private static string user = null;
        private static string modifier = null;
        private static string startTime = null;
        private static string remoteServer = null;
        private static bool hide = false;

        static void Main(string[] args) {
            if (args.Length > 0)
            {
                if (args[0] == "/help" || args[0] == "-h" || args[0] == "/h" || args[0] == "help")
                {
                    Helper.PrintHelp();
                    return;
                }
                Dictionary<string, string> argsParam = Helper.ParseArgs(args);
                if (argsParam == null)
                    return;
                if (argsParam.ContainsKey("method"))
                    method = argsParam["method"];
                if (argsParam.ContainsKey("taskname"))
                    taskName = argsParam["taskname"];
                if (argsParam.ContainsKey("folder"))
                    folder = argsParam["folder"];
                if (argsParam.ContainsKey("author"))
                    author = argsParam["author"];
                if (argsParam.ContainsKey("description"))
                    description = argsParam["description"];
                if (argsParam.ContainsKey("trigger"))
                    trigger = argsParam["trigger"];
                if (argsParam.ContainsKey("program"))
                    program = argsParam["program"];
                if (argsParam.ContainsKey("argument"))
                    agrument = argsParam["argument"];
                if (argsParam.ContainsKey("user"))
                    user = argsParam["user"];
                if (argsParam.ContainsKey("modifier"))
                    modifier = argsParam["modifier"];
                if (argsParam.ContainsKey("starttime"))
                    startTime = argsParam["starttime"];
                if (argsParam.ContainsKey("remoteserver"))
                    remoteServer = argsParam["remoteserver"];
                if (argsParam.ContainsKey("technique"))
                {
                    string technique = argsParam["technique"];
                    if (technique.Contains("hide"))
                        hide = true;
                }

                new TaskManager(method, taskName, folder, author, description, trigger, program, agrument, user, modifier, startTime, remoteServer, hide);
            }
            else
            {
                Helper.PrintHelp();
                return;
            }
        }
    }
}
