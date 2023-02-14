using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Pfim;
using ImageFormat = Pfim.ImageFormat;

namespace dds2png
{
    public class Arguments
    {
        public bool Quiet = false;
        public bool ShowHelp = false;
        public bool ShowVersion = false;
        public bool SkipExisting = false;
        public List<string> Input = new List<string>();
        public List<string> Output = new List<string>();
        public static Arguments ProcessArguments(string[] args)
        {
            Arguments _args = new Arguments();
            for (int i = 0; i < args.Length; i++)
            {
                var item = args[i];
                switch (item.ToUpper())
                {
                    case "-I":
                    case "--INPUT":
                        i++;
                        item = args[i];
                        _args.Input.Add(item);
                        break;
                    case "-O":
                    case "--OUTPUT":
                        i++;
                        item = args[i];
                        _args.Output.Add(item);
                        break;
                    case "-V":
                    case "--VERSION":
                        _args.ShowVersion = true;
                        break;
                    case "-H":
                    case "--HELP":
                        _args.ShowHelp = true;
                        break;
                    case "-Q":
                    case "--QUIET":
                        _args.Quiet = true;
                        break;
                    case "-S":
                    case "--SKIP-EXISTING":
                        _args.SkipExisting = true;
                        break;

                }
            }
            return _args;
        }
    }
    public static class Output
    {
        public static bool Quiet = false;
        public static void WriteLine(object obj) { if (!Quiet) Console.WriteLine(obj.ToString()); }
        public static void WriteLine() { if (!Quiet) Console.WriteLine(); }
        public static void Write(object obj) { if (!Quiet) Console.Write(obj.ToString()); }
        public static void SetForeground(ConsoleColor color) { if (!Quiet) Console.ForegroundColor = color; }
        public static void ResetColor() { if (!Quiet) Console.ResetColor(); }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var a = Arguments.ProcessArguments(args);
            Output.Quiet = a.Quiet;
            Output.WriteLine("Copyright (C) 2023 Creeper Lv.");
            Output.WriteLine("Licensed under The MIT License.");
            Output.WriteLine("This program used Pfim library.");
            if (a.ShowHelp)
            {
                Output.WriteLine("DDS2PNG [options]");
                Output.WriteLine("Options:");
                Output.WriteLine("\t-I FILE/FOLDER");
                Output.WriteLine("\t\tInput files. Try use double asterisk. ");
                Output.WriteLine("\t-O FOLDER");
                Output.WriteLine("\t-Q");
                Output.WriteLine("\t\tQuiet mode.");
                Output.WriteLine("\t-H");
                Output.WriteLine("\t\tShow this help.");
                Output.WriteLine("\t-V");
                Output.WriteLine("\t\tShow version info.");
                Output.WriteLine("\t-S");
                Output.WriteLine("\t\tSkip existing converted files.");
                Output.WriteLine("Example:");
                Output.WriteLine("\tdds2png -i ../../samples/**/*.dds -o ../../output");
            }
            if (a.ShowVersion)
            {
                var dds2png = typeof(Program).Assembly;
                var Pfim = typeof(Pfimage).Assembly;
                Output.Write("DDS2PNG:");
                Output.SetForeground(ConsoleColor.Green);
                Output.Write("" + dds2png.GetName().Version);
                Output.ResetColor();
                Output.WriteLine();
                Output.Write("Pfim:");
                Output.SetForeground(ConsoleColor.Green);
                Output.Write("" + Pfim.GetName().Version);
                Output.ResetColor();
                Output.WriteLine();
            }
            for (int i = 0; i < a.Input.Count; i++)
            {
                string output = null;
                if (a.Output.Count > 0)
                {
                    a.Output.Last();
                }
                if (a.Output.Count > i)
                {
                    output = a.Output[i];
                }
                INPUT(a.Input[i], output,a.SkipExisting);
            }
        }
        static void DOUBLE_ASTERISK(DirectoryInfo BaseDir, DirectoryInfo dir, string name, string Output, bool SkipExisting)
        {
            var SubD = dir.EnumerateDirectories();
            var SubF = dir.EnumerateFiles(name);
            foreach (var item in SubD)
            {
                DOUBLE_ASTERISK(BaseDir, item, name, Output, SkipExisting);
            }
            foreach (var item in SubF)
            {
                string output = Output;
                output = Path.Combine(output, Path.GetRelativePath(BaseDir.FullName, item.FullName));
                PROCESS_SINGLE_FILE(item.FullName, output, SkipExisting);
            }
        }
        static unsafe void PROCESS_SINGLE_FILE(string file, string output_file, bool SkipExisting)
        {
            Output.SetForeground(ConsoleColor.Green);
            Output.Write(file);
            Output.ResetColor();
            Output.Write("->");
            Output.SetForeground(ConsoleColor.Green);
            Output.Write(output_file);
            Output.ResetColor();
            Output.WriteLine();
            try
            {
                var _o = Path.ChangeExtension(output_file, ".png");
                var fi = new FileInfo(_o);
                if (!fi.Directory.Exists) fi.Directory.Create();
                if (fi.Exists)
                {
                    if (SkipExisting)
                    {
                        Output.SetForeground(ConsoleColor.Yellow);
                        Output.WriteLine("Skipped.");
                        Output.ResetColor();
                        return;
                    }
                    fi.Delete();
                }
                using var image = Pfimage.FromFile(file);

                var format = image.Format switch
                {
                    ImageFormat.Rgba32 => PixelFormat.Format32bppArgb,
                    _ => throw new NotImplementedException(),
                };
                var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                try
                {
                    var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                    var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                    bitmap.Save(_o, System.Drawing.Imaging.ImageFormat.Png);
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception e)
            {
                Output.SetForeground(ConsoleColor.Red);
                Output.WriteLine("Error:");
                Output.WriteLine(e);
                Output.ResetColor();
            }
        }
        static void REC_INPUT(DirectoryInfo dir, List<string> PathSegment, string _Output, bool SkipExisting)
        {
            DirectoryInfo DI = dir;
            if (DI == null)
            {
                if (PathSegment[0] == "")
                {
                    DI = new DirectoryInfo("/");
                }
                else
                {
                    DI = new DirectoryInfo(PathSegment[0]);
                }
                PathSegment.RemoveAt(0);
            }
            var item = PathSegment.First();
            if (PathSegment.Count == 1)
            {
                var fs = DI.EnumerateFiles(item);
                foreach (var _file in fs)
                {
                    string output = _Output;
                    output = Path.Combine(output, _file.Name);
                    PROCESS_SINGLE_FILE(_file.FullName, output, SkipExisting);
                }
            }
            else
            {

                if (item == "**")
                {
                    DOUBLE_ASTERISK(DI, DI, PathSegment[1], _Output, SkipExisting);
                }
                else
                {
                    Output.WriteLine("search:" + item);
                    var _folders = DI.EnumerateDirectories(item);
                    foreach (var _folder in _folders)
                    {
                        PathSegment.RemoveAt(0);
                        REC_INPUT(_folder, PathSegment, _Output, SkipExisting);
                    }
                }
            }
        }
        static void INPUT(string input, string output, bool SkipExisting)
        {
            var segments = input.Split('/', '\\');
            REC_INPUT(null, segments.ToList(), output, SkipExisting);
        }
    }
}