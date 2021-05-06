using DustInTheWind.ConsoleTools.InputControls;
using DustInTheWind.ConsoleTools.Menues;
using System;
using System.Collections.Generic;
using DustInTheWind.ConsoleTools;
using System.Diagnostics;
using System.Text;
using static CxAPI_Store.CxConstant;

namespace CxAPI_Store
{

    public class SQLLauncher : ICommand
    {
        private string directory;
        private string command;
        private resultClass token;

        public SQLLauncher(resultClass token, string cmdName)
        {
            this.token = token;
            directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            command = cmdName;
        }
        public bool IsActive => true;

        public void Execute()
        {
            if (command.Contains("Exit"))
            {
                Environment.Exit(0);
                return;
            }
            string baseConnection;
            if (token.test)
            {
                Console.WriteLine("WARNING: Using test database.");
                baseConnection = String.Format("{0}{1}sqlite-tools{1}{2}", token.exe_directory, token.os_path, TestDB);
            }
            else
            {
                var sqlite = token.sqlite_connection;
                string[] split = sqlite.Split(';');
                string[] path = split[0].Split('=');
                baseConnection = path[1];
            }
            Process pEditor;
            var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pEditor = new Process();
            pEditor.StartInfo.FileName = directory + token.os_path + "sqlite-tools" + token.os_path + command;
            pEditor.StartInfo.Arguments = command.Contains("sqldiff") ? null : baseConnection;   
            pEditor.Start();
            pEditor.WaitForExit();
        }
    }

    public class ToolSet : TextMenu
    {
        private resultClass token;
        public ToolSet(resultClass token)
        {
            this.token = token;
            DisplayMenu();
        }
        private void DisplayMenu()
        {

            EraseAfterClose = true;
            Margin = "0 1";

            TitleText = "Helper Applications";
            TitleForegroundColor = ConsoleColor.Cyan;

            IEnumerable<TextMenuItem> menuItems = CreateMenuItems();
            AddItems(menuItems);

        }

        private IEnumerable<TextMenuItem> CreateMenuItems()
        {
            return new[]
            {
                new TextMenuItem
                {
                    Id = "1",
                    Text = "SQLite Command Line",
                    Command = new SQLLauncher(token,"sqlite3.exe")
                },
                new TextMenuItem
                {
                    Id = "2",
                    Text = "SQLite Diff Tool",
                    Command = new SQLLauncher(token,"sqldiff.exe")
                },
                new TextMenuItem
                {
                    Id = "3",
                    Text = "SQLite Analyzer",
                    Command = new SQLLauncher(token,"sqlite3_analyzer")
                },
                new TextMenuItem
                {
                    Id = "4",
                    Text = "Quit",
                    Command = new SQLLauncher(token,"Exit")
                }

            };

        }

    }
    public static class runMenu
    {
      
        public static void startMenu(resultClass token)
        {
            Console.Clear();
            DisplayApplicationHeader();
            //Console.SetWindowSize(80, 50);
            //Console.SetBufferSize(80, 50);

            //Console.CancelKeyPress += HandleCancelKeyPress;

            var mainMenuRepeater = new ControlRepeater
            {
                Control = new ToolSet(token)
            };

            //gameApplication.Exited += HandleGameApplicationExited;

            mainMenuRepeater.Display();

        }
        private static void DisplayApplicationHeader()
        {
            CustomConsole.WriteLineEmphasies("ConsoleTools - TextMenu");
            CustomConsole.WriteLineEmphasies("===============================================================================");
            CustomConsole.WriteLine();
            CustomConsole.WriteLine("This displays the helper utilities that can be accessed locally.");
            CustomConsole.WriteLine("Enter the number of the utility or quit to exit");
            CustomConsole.WriteLine();
            CustomConsole.WriteLine();
            CustomConsole.WriteLine();
        }

    }
}



