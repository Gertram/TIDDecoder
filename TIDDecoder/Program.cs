using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace TIDDecoder
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        static void ImportImages(List<string> args)
        {
            var tasks = new Task[args.Count];
            var num = 0;
            foreach(var tidFile in args.Where(x=>!x.EndsWith(".png")))
            {
                var pngFile = Path.ChangeExtension(tidFile, ".png");
                var task = new Task(delegate {
                    try
                    {
                        Console.WriteLine(tidFile);

                        Form1.ImportFile(tidFile, pngFile);

                        Console.WriteLine("Imported: "+tidFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                });
                task.Start();
                tasks[num] = task;
                num++;
            }
            Task.WaitAll(tasks);
        }
        //static void ExportImages(List<string> args)
        //{
        //    var tasks = new Task[args.Count];
        //    var num = 0;
        //    foreach (var arg in args)
        //    {
        //        var task = new Task(delegate {
        //            try
        //            {
        //                Console.WriteLine(arg);

        //                var filename = Path.Combine(Path.GetDirectoryName(arg), Path.ChangeExtension(Path.GetFileName(arg), "png"));

        //                Form1.DecodeFile(arg, filename);

        //                Console.WriteLine("Created ", filename);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.ToString());
        //            }
        //        });
        //        task.Start();
        //        tasks[num] = task;
        //        num++;
        //    }
        //    Task.WaitAll(tasks);
        //}
        static void HandleConsole(List<string> args)
        {

            //if (args[0] == "-import")
            //{
            //    args.RemoveAt(0);
            //    ImportImages(args);
            //    return;
            //}
            //if (args[1] == "-export")
            //{
            //    args.RemoveAt(0);
            //    ExportImages(args);
            //    return;
            //}
            //ExportImages(args);
            if (!Form1.HandleFiles(args))
            {
                Console.ReadKey();
            }
        }
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AllocConsole();
            if (args.Length > 0)
            {
                HandleConsole(args.ToList());
                return;
            }
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
