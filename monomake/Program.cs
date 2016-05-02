using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace monomake
{
    class Program
    {
        static string AppName = "monomake";
        static string version = "1.0";
        static string templateDir = "template";
        static string MonoprogPath = "monoprog/monoprog.exe";
        static string EnvironmentDir;
        static string[] projectFiles = { templateDir + "/app_controller.h", templateDir + "/app_controller.cpp" };

        static void Main(string[] args)
        {
            var ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///","");
            EnvironmentDir = Path.GetDirectoryName(Path.GetDirectoryName(ExecutablePath));

            if (args.Count() > 0)
            {
                var command = args[0];
                switch (command)
                {
                    case "project":
                        projectCommand(args.Count() >= 2 ? args[1] : null);
                        break;
                    case "monoprog":
                        {
                            var progArgs = args.Skip(1);
                            runMonoprog(progArgs);
                            break;
                        }
                    case "help":
                        showhelp();
                        break;
                    case "version":
                        printVersion();
                        break;
                    case "path":
                        Console.WriteLine("Mono Environment Path: {0}", EnvironmentDir);
                        break;
                    case "-p":
                        programElf(args[1]);
                        break;
                    case "reboot":
                        rebootMonoDtr();
                        break;
                    case "writemake":
                        WriteMakefile(args[1], "Makefile");
                        break;
                    default:
                        Console.Error.WriteLine("Unkown command: {0}",command);
                        break;
                }
            }
            else
            {
                showArgError();
            }

#if DEBUG
            Console.ReadKey();
#endif
        }


        static void showArgError()
        {
            Console.Error.WriteLine("ERR: No command argument given! You must provide a command");
            showhelp();
        }

        static void runMonoprog(IEnumerable<string> args)
        {
            var info = new ProcessStartInfo();
            info.FileName = EnvironmentDir + "/" + MonoprogPath;
            info.Arguments = String.Join(" ", args);
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            using (var p = Process.Start(info))
            {
                Console.Write(p.StandardOutput.ReadToEnd());
                p.WaitForExit();
            }
        }

        static void programElf(string elfFile)
        {
            var args = new[]{ "-p", elfFile, "--verbose=2" };
            runMonoprog(args);
        }

        static void rebootMonoDtr()
        {
            var info = new ProcessStartInfo();
            info.FileName = EnvironmentDir + "/reset.exe";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            using (var p = Process.Start(info))
            {
                Console.Write(p.StandardOutput.ReadToEnd());
                p.WaitForExit();
            }
        }

        static void createProjectFolder(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                Console.Error.WriteLine("No project name given!");
                return;
            }
            else if (Directory.Exists(name))
            {
                Console.Error.WriteLine("Project target directory already exists!");
                return;
            }

            Console.WriteLine("Creating new mono project: {0}...", name);

            try
            {
                Directory.CreateDirectory(name);

                foreach (var file in projectFiles)
                {
                    Console.WriteLine(" * {0}/{1}", name, Path.GetFileName(file));
                    File.Copy(EnvironmentDir + "/" + file, name + "/" + Path.GetFileName(file), false);
                }

                WriteMakefile(name, name + "/Makefile");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed: " + e.Message);
                return;
            }

        }

        static void WriteMakefile(string name, string filePath)
        {
            var text = new StringBuilder();
            text.AppendFormat("\n# Makefile created by {0}, {1}\n", AppName, DateTime.Now.ToString());
            text.AppendFormat("# Project: {0}\n\n", name);
            text.AppendFormat("MONO_PATH={0}\n", EnvironmentDir.Replace("\\","/"));
            text.AppendFormat("include $(MONO_PATH)/predefines.mk\n\n");
            text.AppendFormat("TARGET={0}\n\n", name);
            text.AppendFormat("include $(MONO_PATH)/mono.mk\n");

            Console.WriteLine("Writing Makefile: {0}...", filePath);
            File.WriteAllText(filePath, text.ToString());
        }

        static void projectCommand(string name = "new_mono_project")
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                createProjectFolder("new_mono_project");
            }
            else
            {
                createProjectFolder(name);
            }
        }

        static void showhelp()
        {
            var output = new StringBuilder();
            output.Append("OpenMono project PowerShell utility, creating new projects and access to monoprog\n\n");
            
            output.AppendFormat ("Usage:\n{0} COMMAND [options]\n\n",AppName);
            output.Append       ("Commands:\n");
            output.Append       ("  project [name]  Create a new project folder. Default name is: new_mono_project\n");
            //Write-Host "  bootldr         See if a mono is connected and in bootloader"
            output.Append       ("  monoprog [...]  Shortcut to access the MonoProg USB programmer\n");
            output.Append       ("  -p ELF_FILE     Upload an application to mono\n");
            output.Append       ("  reboot          Send Reboot command to Mono, using the Arduino DTR method\n");
            output.AppendFormat ("  version         Display the current version of {0}\n",AppName);
            output.Append       ("  path            Display the path to the Mono Environment installation dir\n");

            Console.Write(output.ToString());
        }
        

        static void printVersion()
        {
            Console.WriteLine("{0} version {1}", AppName, version);
        }


    }
}
