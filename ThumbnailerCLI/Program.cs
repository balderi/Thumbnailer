using libthumbnailer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ThumbnailerCLI
{
    class Program
    {
        static bool overwrite, recurse, verbose;
        static string sourcePath, configPath;
        static List<ContactSheet> sheets;
        static int curFile, totalFiles;
        static Config config;

        static async Task Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Too few arguments. Use -h or --help for help.");
                return;
            }

            sheets = new List<ContactSheet>();

            foreach(string arg in args)
            {
                if(arg[0] == '-')
                {
                    try
                    {
                        ParseFlag(arg);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Environment.Exit(1);
                    }
                }
                else if(sourcePath is null)
                {
                    sourcePath = arg;
                }
                else if(configPath is null)
                {
                    configPath = arg;
                }
            }

            if (configPath is null)
                configPath = "default.xml";

            config = Config.Load(configPath);

            List<string> files = (List<string>)Loader.LoadFiles(sourcePath, recurse);
            totalFiles = files.Count;
            //logger.LogInfo($"Building {totalFiles} sheets...");
            PrintMsg($"Building {totalFiles} sheets...\n");
            int count = 0;
            foreach (var f in files)
            {
                var sheet = ContactSheetFactory.CreateContactSheet(f, logger);
                if (verbose)
                    sheet.SheetPrinted += SheetPrinted;
                sheets.Add(sheet);
                PrintOverMsg($"Built {++count}/{totalFiles} sheets...");
            }

            curFile = 0;

            //ContactSheet.AllSheetsPrinted += AllSheetsPrinted;
            //logger.LogInfo($"Printing {count} sheets...");
            PrintMsg($"Printing {count} sheets...\n");
            //ContactSheet.PrintSheetsParallel(sheets, config, logger, overwrite: overwrite);

            //Wait forever until the AllSheetsPrinted event is fired
            await Task.Delay(-1);
        }

        static void SheetPrinted(object sender, string e)
        {
            ++curFile;
            var s = sender as ContactSheet;
            s.SheetPrinted -= SheetPrinted;
            PrintOverMsg($"Printed {curFile}/{totalFiles} sheets...");
        }

        static void AllSheetsPrinted(object sender, string e)
        {
            //ContactSheet.AllSheetsPrinted -= AllSheetsPrinted;
            CleanUp();
        }

        static void CleanUp()
        {
            //logger.LogInfo($"Beginning clean-up...");
            PrintMsg($"Beginning clean-up...");
            foreach (string d in Directory.GetDirectories("temp"))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch (Exception ex)
                    {
                        //logger.LogWarning($"Failed to delete file {f}: {ex.Message}");
                    }
                }
                try
                {
                    Directory.Delete(d);
                }
                catch (Exception ex)
                {
                    //logger.LogWarning($"Failed to delete directory {d}: {ex.Message}");
                }
            }
            //logger.LogInfo($"Clean-up done!");
            PrintMsg($"Clean-up done!");
            //logger.Close();
            Environment.Exit(0);
        }

        static void ParseFlag(string flag)
        {
            if(flag[1] == '-')
            {
                switch (flag)
                {
                    case "--overwrite":
                        {
                            overwrite = true;
                            break;
                        }
                    case "--recursive":
                        {
                            recurse = true;
                            break;
                        }
                    case "--verbose":
                        {
                            verbose = true;
                            break;
                        }
                    case "--help":
                        {
                            Console.WriteLine("usage: ThumbnailerCLI [options] <source file/folder> [config]");
                            Environment.Exit(0);
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException($"Invalid option '{flag[2..]}'.");
                        }
                }
            }
            else
            {
                var substr = flag[1..];
                foreach (var f in substr)
                {
                    switch (f)
                    {
                        case 'o':
                            {
                                overwrite = true;
                                break;
                            }
                        case 'r':
                            {
                                recurse = true;
                                break;
                            }
                        case 'v':
                            {
                                verbose = true;
                                break;
                            }
                        case 'h':
                            {
                                Console.WriteLine("usage: ThumbnailerCLI [options] <source file/folder> [config]");
                                Environment.Exit(0);
                                break;
                            }
                        default:
                            {
                                throw new ArgumentException($"Invalid option '{f}'.");
                            }
                    }
                }
            }
        }

        static void PrintMsg(string msg)
        {
            if(verbose)
                Console.WriteLine(msg);
        }

        static void PrintOverMsg(string msg)
        {
            if(verbose)
            {
                Console.CursorLeft = 0;
                Console.CursorTop--;
                Console.WriteLine(msg);
            }
        }
    }
}
