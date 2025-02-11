# ScheduleRunner: A Flexible C# Tool for Red Team Operations

**ScheduleRunner** is a versatile tool for creating, managing, and evading scheduled tasks during red team operations. Designed with flexibility in mind, this C# tool provides customizations not available in other existing tools, making it ideal for **persistence** and **lateral movement** tactics. It is compatible with **CobaltStrike execute-assembly**, allowing for seamless integration into offensive security workflows.

![C#](https://img.shields.io/badge/Language-C%23-Blue)

## Features

- **Create** and manage scheduled tasks with full customization.
- **Lateral Movement**: Automatically create, run, and delete tasks on remote systems.
- **Evasion**: Use advanced techniques to hide scheduled tasks from detection tools like Task Scheduler.
- **Remote Execution**: Run tasks on remote servers to extend your attack surface.

## Screenshot

![HowTo](https://github.com/netero1010/ScheduleRunner/raw/main/screenshot.png)

## Table of Contents

- [Methods](#methods)
- [Options for Task Creation](#options-for-scheduled-task-creation)
- [Options for Task Deletion](#options-for-scheduled-task-deletion)
- [Options for Task Execution](#options-for-scheduled-task-execution)
- [Options for Task Query](#options-for-scheduled-task-query)
- [Lateral Movement](#options-for-scheduled-task-lateral-movement)
- [Example Usage](#example-usage)
- [Hiding Scheduled Task Technique](#hiding-scheduled-task-technique)
- [Libraries and References](#library-and-reference-used)

## Methods

ScheduleRunner supports several methods for managing scheduled tasks:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| `create`       | Create a new scheduled task                                                |
| `delete`       | Delete an existing scheduled task                                          |
| `run`          | Execute a specified scheduled task                                         |
| `query`        | Retrieve details about a scheduled task or all tasks in a specific folder  |
| `queryfolders` | List all sub-folders in the scheduled task library                         |
| `move`         | Perform lateral movement by creating, running, and deleting tasks remotely |

## Options for Scheduled Task Creation (/method:create)

Create a new scheduled task with various customizable options:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| [*] `/taskname` | The name of the scheduled task                                            |
| [*] `/program`  | The program to run when the task executes                                 |
| [*] `/trigger`  | The schedule type: `minute`, `hourly`, `daily`, `weekly`, `onstart`, `onlogon`, `onidle` |
| `/modifier`     | Frequency modifier for certain schedule types (e.g., `1-1439 minutes` for `minute`, `1-23 hours` for `hourly`) |
| `/starttime`    | Start time for daily schedules (e.g., `23:30`)                            |
| `/argument`     | Command line arguments for the program                                   |
| `/folder`       | Folder to store the scheduled task (default: `\\`)                        |
| `/workingdir`   | Working directory for task execution                                     |
| `/author`       | Author of the scheduled task                                              |
| `/description`  | Description of the scheduled task                                        |
| `/remoteserver` | Hostname or IP address of a remote computer                               |
| `/user`         | User account to run the task                                              |
| `/technique`    | Evasion technique (`hide` for hiding tasks from Task Scheduler)           |

### [*] Required Fields

## Options for Scheduled Task Deletion (/method:delete)

Delete a specified scheduled task:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| [*] `/taskname` | The name of the scheduled task                                            |
| `/folder`       | Folder to store the scheduled task (default: `\\`)                        |
| `/remoteserver` | Hostname or IP address of a remote computer                               |
| `/technique`    | Evasion technique (`hide` for hidden tasks)                               |

### [*] Required Fields

## Options for Scheduled Task Execution (/method:run)

Execute a scheduled task:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| [*] `/taskname` | The name of the scheduled task                                            |
| `/folder`       | Folder to store the scheduled task (default: `\\`)                        |
| `/remoteserver` | Hostname or IP address of a remote computer                               |

### [*] Required Fields

## Options for Scheduled Task Query (/method:query)

Query details for a specific task:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| `/taskname`    | The name of the scheduled task                                            |
| `/folder`       | Folder to store the scheduled task (default: `\\`)                        |
| `/remoteserver` | Hostname or IP address of a remote computer                               |

### [*] Required Fields

## Options for Scheduled Task Lateral Movement (/method:move)

Perform lateral movement by creating, running, and deleting tasks on a remote server:

| **Method**     | **Function**                                                              |
| -------------- | ------------------------------------------------------------------------- |
| [*] `/taskname` | The name of the scheduled task                                            |
| [*] `/program`  | Program to execute when the task runs                                     |
| [*] `/remoteserver` | Hostname or IP address of a remote computer                            |
| `/trigger`      | Schedule type: `minute`, `hourly`, `daily`, `weekly`, `onstart`, `onlogon`, `onidle` |
| `/modifier`     | Frequency modifier for certain schedule types (e.g., `1-1439 minutes`)   |
| `/starttime`    | Start time for daily schedules (e.g., `23:30`)                            |
| `/argument`     | Command line arguments for the program                                   |
| `/folder`       | Folder to store the scheduled task (default: `\\`)                        |
| `/workingdir`   | Working directory for task execution                                     |
| `/author`       | Author of the scheduled task                                              |
| `/description`  | Description of the scheduled task                                        |
| `/user`         | User account to run the task                                              |

### [*] Required Fields

## Example Usage

Here are some example commands for creating, querying, deleting, and moving scheduled tasks:

- **Create a scheduled task "Cleanup" that runs daily at 11:30 p.m.**
  
  ```bash
  ScheduleRunner.exe /method:create /taskname:Cleanup /trigger:daily /starttime:23:30 /program:calc.exe /description:"Some description" /author:netero1010
  ```

- **Create a scheduled task "Cleanup" every 4 hours on a remote server.**

  ```bash
  ScheduleRunner.exe /method:create /taskname:Cleanup /trigger:hourly /modifier:4 /program:rundll32.exe /argument:c:\temp\payload.dll /remoteserver:TARGET-PC01
  ```

- **Delete a scheduled task "Cleanup".**

  ```bash
  ScheduleRunner.exe /method:delete /taskname:Cleanup
  ```

- **Execute the scheduled task "Cleanup".**

  ```bash
  ScheduleRunner.exe /method:run /taskname:Cleanup
  ```

## Hiding Scheduled Task Technique

This technique, used by **HAFNIUM** malware, hides the task from Task Scheduler and makes it unqueriable by regular tools. The following actions are performed to hide a task:

1. Delete the "SD" value from `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\[task name]`
2. Remove the XML file located at `C:\Windows\System32\Tasks\[task name]`

### Limitations

The task remains active until the next system reboot, even after deletion. Avoid using this technique on servers that will be rebooted frequently.

### Demo

![HowTo](https://github.com/netero1010/ScheduleRunner/raw/main/hiding_scheduled_task.png)

## Libraries and References Used

| **Library**     | **Link**                                                                  |
| --------------  | ------------------------------------------------------------------------- |
| `TaskScheduler` | [GitHub - TaskScheduler](https://github.com/dahall/TaskScheduler)         |

| **Reference**   | **Link**                                                                  |
| --------------  | ------------------------------------------------------------------------- |
| `SharpPersist`  | [GitHub - SharpPersist](https://github.com/mandiant/SharPersist)          |
| `Hiding Technique` | [Microsoft Blog on Tarrask](https://www.microsoft.com/security/blog/2022/04/12/tarrask-malware-uses-scheduled-tasks-for-defense-evasion/) |

---

## Contribution

Feel free to fork this repository and contribute. For issues, questions, or suggestions, please use the [Issues](https://github.com/netero1010/ScheduleRunner/issues) tab.
