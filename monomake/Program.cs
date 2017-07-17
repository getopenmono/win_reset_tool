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
        static string version = "1.5";
        static string templateDir = "template";
        static string MonoprogPath = "monoprog/monoprog.exe";
        static string GccIncludePath = "gcc-arm-none-eabi-5_2-2015q4-20151219-win32/arm-none-eabi/include";
        static string EnvironmentDir;
        static string[] projectFiles = { templateDir + "/app_controller.h", templateDir + "/app_controller.cpp" };
        static string[] bareProjectFiles = { templateDir + "/app_controller_bare.h", templateDir + "/app_controller_bare.cpp" };
        static string AutoCompleteIncludes = @"-I{0}/{1}
-I{0}/{1}/c++/5.2.1/arm-none-eabi/thumb
-I{0}/{1}/c++/5.2.1/arm-none-eabi
-I{0}/{1}/c++/5.2.1
-I{0}/mono/include
-I{0}/mono/include/display
-I{0}/mono/include/display/ui
-I{0}/mono/include/display/ili9225g
-I{0}/mono/include/io
-I{0}/mono/include/media
-I{0}/mono/include/mbed/api
-I{0}/mono/include/mbed/hal
-I{0}/mono/include/mbed/libraries/fs/sd
-I{0}/mono/include/mbed/target_cypress";

        static void Main(string[] args)
        {
            var ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///","");
            EnvironmentDir = Path.GetDirectoryName(Path.GetDirectoryName(ExecutablePath));

            if (args.Count() > 0)
            {
                var command = args[0];

				if (command == "-c")
				{
                    Console.WriteLine("Running with Environment Dir: {0}", args[1]);
					EnvironmentDir = args [1];
					command = args [2];
					args = args.Skip (2).ToArray();
				}

                switch (command)
                {
                    case "project":
                        if (args.Count() >= 2)
                        {
                            if (args[1] == "--bare")
                                projectCommand(args.Count() >= 3 ? args[2] : null, true);
                            else if (args.Count() >= 3 && args[2] == "--bare")
                                projectCommand(args[1], true);
                            else
                                projectCommand(args[1]);
                        }
                        else
                            projectCommand(null);
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
                        printVersion(args.Length >= 2 && args[1] == "--bare");
                        break;
                    case "path":
                        if (args.Length >= 2 && args[1] == "--bare")
                            Console.WriteLine(EnvironmentDir);
                        else
                            Console.WriteLine("Mono Environment Path: {0}", EnvironmentDir);
                        break;
                    case "-p":
                        Console.WriteLine("Note: -p is deprecated, use program instead!");
                        programElf(args[1]);
                        break;
                    case "program":
                        programElf(args[1]);
                        break;
                    case "detect":
                        runMonoprog(new[] { "-d" });
                        break;
                    case "reboot":
                        rebootMonoDtr();
                        break;
                    case "serial":
                        listSerials();
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
            info.RedirectStandardError = true;
            var p = new Process { StartInfo = info };

            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                Console.Write("{0}\n",e.Data);
            };

            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                Console.Write("{0}\n",e.Data);
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();
            Console.Out.Write(Console.Out.NewLine);
            p.Dispose();
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
            info.RedirectStandardError = true;
            using (var p = Process.Start(info))
            {
                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    Console.Write("{0}\n",e.Data);
                };

                p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    Console.Write("{0}\n",e.Data);
                };

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }
        }

        static void listSerials()
        {
            var info = new ProcessStartInfo();
            info.FileName = EnvironmentDir + "/reset.exe";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.Arguments = "--list --bare";
            using (var p = Process.Start(info))
            {
                Console.Write(p.StandardOutput.ReadToEnd());
                p.WaitForExit();
            }
        }

        static void createProjectFolder(string name, bool bare = false)
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

            Console.WriteLine("Creating new {0}mono project: {1}...", bare?"bare ":"",name);

            try
            {
                Directory.CreateDirectory(name);

                string[] projFiles = bare ? bareProjectFiles : projectFiles;

                foreach (var file in projFiles)
                {
                    Console.WriteLine(" * {0}/{1}", name, Path.GetFileName(file).Replace("_bare",""));
                    File.Copy(EnvironmentDir + "/" + file, name + "/" + Path.GetFileName(file).Replace("_bare",""), false);
                }

                WriteMakefile(name, name + "/Makefile");
				writeAtomProjectFiles(name);
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
            text.AppendFormat("MONO_PATH=$(subst \\,/,$(shell monomake path --bare))\n");
            text.AppendFormat("include $(MONO_PATH)/predefines.mk\n\n");
            text.AppendFormat("TARGET={0}\n\n", name);
            text.AppendFormat("include $(MONO_PATH)/mono.mk\n");

            Console.WriteLine("Writing Makefile: {0}...", filePath);
            File.WriteAllText(filePath, text.ToString());
        }

		static void writeAtomProjectFiles(string filePath)
		{
			var text = String.Format (AutoCompleteIncludes, EnvironmentDir, GccIncludePath);
			Console.WriteLine ("Atom Project Settings: Writing Auto complete includes...");
			File.WriteAllText (filePath + "/.clang_complete", text);
		}

        static void projectCommand(string name = "new_mono_project", bool bare = false)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                createProjectFolder("new_mono_project", bare);
            }
            else
            {
                createProjectFolder(name, bare);
            }
        }

        static void showhelp()
        {
            var output = new StringBuilder();
            output.Append("OpenMono SDK command line utility, for creating new projects and access to monoprog\n\n");

            output.AppendFormat ("Usage:\n{0} COMMAND [options]\n\n",AppName);
            output.Append       ("Commands:\n");
            output.Append       ("  project [--bare] [name]  Create a new project folder. Default name is: new_mono_project\n");
            output.Append       ("  monoprog [...]           Shortcut to access the MonoProg USB programmer\n");
            output.Append       ("  program ELF_FILE         Upload an application to mono\n");
            output.Append       ("  reboot                   Send Reboot command to Mono, using the Arduino DTR method\n");
            output.Append       ("  detect                   See if a mono is connected and if it is in bootloader\n");
            output.Append       ("  serial                   Return Mono's serial device COM port\n");
            output.AppendFormat ("  version [--bare]         Display the current version of {0}\n", AppName);
            output.Append       ("  path [--bare]            Display the path to the Mono Environment installation dir\n");

            Console.Write(output.ToString());
        }


        static void printVersion(bool bare = false)
        {
            if (bare)
                Console.WriteLine(version);
            else
                Console.WriteLine("{0} version {1}", AppName, version);
        }


    }
}
