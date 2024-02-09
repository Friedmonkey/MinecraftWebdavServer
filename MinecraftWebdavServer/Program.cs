using System.Diagnostics;
using System.IO;

internal class Program
{
    public static readonly string BasePath = @"C:\MinecraftServer\ServerName";
    public static readonly string DriveLetter = "Z";
    public static readonly TimeSpan StartupRecentEnough = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ClosingRecentEnough = TimeSpan.FromMinutes(40);
    private static void Main(string[] args)
    {
        if (args.Length != 1)
            args = new string[] { "-Run" };

        Confirmation:
        Console.WriteLine($"Program is set to \"{args[0]}\", Do you want to execute? [y/n]");
        var opt = Console.ReadLine();
        OptCheck:
        if (opt.ToLower() != "y" && opt.ToLower() != string.Empty)
        { 
            ListOptions();
            Console.WriteLine("What else do you want to run? (e.g. \"-download\")");
            var newopt = Console.ReadLine();
            args = new string[] { newopt };
            goto Confirmation;
        }


        try
        {
            switch (args[0].ToLower())
            {
                case "-run":
                    Run();
                    break;
                case "-download":
                    Download();
                    break;
                case "-upload":
                    {
                        var backup = GetLatestBackup();
                        if (backup == null)
                        {
                            PrintError($"No backups were found in: \"{BasePath}\"");
                            return;
                        }
                        Upload(backup);

                    }
                    break;
                default:
                    PrintError($"Command \"{args[0]}\" not found!");
                    opt = "n";
                    goto OptCheck;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var line in ex.ToString().Split('\n'))
            {
                Console.WriteLine(line);
            }
            Console.ResetColor();

            Console.WriteLine();
            Print("Program closed.",ConsoleColor.Red);
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
    private static string? GetLatestBackup() 
    {
        var backups = Directory.EnumerateDirectories(BasePath).ToList();
        backups.Sort();
        backups.Reverse();
        var backup = backups.FirstOrDefault();
        return backup;
    }
    private static bool BackupIsRecentEnough(TimeSpan RecentEnough)
    {
        try
        {
            var backup = GetLatestBackup();
            if (backup == null) return false;
            var fileName = Path.GetFileName(backup);
            var split1 = fileName.Split(' ');
            var split2 = split1.First().Split('_');
            var date = split2.First();
            var time = split2.Last();
            var dateSegments = date.Split('-');
            var timeSegments = time.Split('-');

            int year = dateSegments[0].ToInt();
            int month = dateSegments[1].ToInt();
            int day = dateSegments[2].ToInt();

            int hours = timeSegments[0].ToInt();
            int minutes = timeSegments[1].ToInt();

            DateTime backupTime = new DateTime(year, month, day, hours, minutes, 59);
            DateTime now = DateTime.Now;
            TimeSpan between = now.Subtract(backupTime);
            if (between > RecentEnough)
                return false;
            else
                return true;
        }
        catch (Exception ex) 
        {
            PrintError($"Backup recent checking failed: {ex.Message}");
            return false;
        }
    }
    private static void ListOptions() 
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Usage:");
        Console.WriteLine("MinecraftWebdavServer -Run");
        Console.WriteLine("MinecraftWebdavServer -Download");
        Console.WriteLine("MinecraftWebdavServer -Upload");
        Console.WriteLine();
        Console.ResetColor();
    }
    private static void Print(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
    private static void PrintError(string error) => Print($"Error: {error}", ConsoleColor.Red);
    private static void PrintTransfer(string file) => Print($"Transfer Complete: {file}", ConsoleColor.Green);
    private static void PrintDirectory(string file) => Print($"Directory Created: {file}", ConsoleColor.Cyan);

    public static void Run() 
    {
        string path = string.Empty;
        bool backupIsRecentEnough = BackupIsRecentEnough(StartupRecentEnough);
        RecentCheck:
        var WASnt = backupIsRecentEnough ? "WAS" : "was NOT";
        var WILLnt = backupIsRecentEnough ? "will NOT" : "WILL";
        Console.WriteLine();
        Print($"The backup {WASnt} recent enough, so saving the files {WILLnt} take long to upload. Do you agree with this? [y/n]", ConsoleColor.Cyan);

        var opt = Console.ReadLine();
        if (opt.ToLower() != "y" && opt.ToLower() != string.Empty)
        {
            backupIsRecentEnough = !backupIsRecentEnough;
            goto RecentCheck;
        }

        if (backupIsRecentEnough)
        {
            Print("Quick Downloading files...", ConsoleColor.Cyan);
            path = QuickDownload();
            if (path == null) return;
            Print("Quick Download Complete!", ConsoleColor.Green);
        }
        else
        {
            Print("Full Downloading ALL files...", ConsoleColor.Blue);
            path = Download();
            if (path == null) return;
            Print("Full Download of ALL files Complete!", ConsoleColor.DarkGreen);
        }

        var batFile = @$"{path}\start_server.bat";

        if (!File.Exists(batFile))
        {
            PrintError($"Start bat with path:\"{batFile}\" not found!");
            return;
        }
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = batFile;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        var result = Process.Start(psi);
        result.WaitForExit();

        bool uploadBackupIsRecentEnough = BackupIsRecentEnough(ClosingRecentEnough);
        UploadRecentCheck:
        var uWASnt = uploadBackupIsRecentEnough ? "WAS" : "was NOT";
        var uWILLnt = uploadBackupIsRecentEnough ? "will NOT" : "WILL";
        Console.WriteLine();
        Print($"The backup {uWASnt} recent enough, so saving the files {uWILLnt} take long to upload. Do you agree with this? [y/n]", ConsoleColor.Cyan);

        var uploadOpt = Console.ReadLine();
        if (uploadOpt.ToLower() != "y" && uploadOpt.ToLower() != string.Empty)
        {
            uploadBackupIsRecentEnough = !uploadBackupIsRecentEnough;
            goto UploadRecentCheck;
        }

        if (uploadBackupIsRecentEnough)
        {
            Print("Quick Uploading files...", ConsoleColor.Cyan);
            QuickUpload(path);
            Print("Quick Uploading Complete!", ConsoleColor.Green);
        }
        else
        {
            Print("Full Uploading ALL files...", ConsoleColor.Blue);
            Upload(path);
            Print("Full Uploading of ALL files Complete!", ConsoleColor.DarkGreen);
        }
    }
    private static bool Parse(ref string input)
    {
        if (input.StartsWith(DriveLetter))
        {
            input = input.Substring(0, input.Length-1);
            if (input.EndsWith("."))
            {
                return true;
            }
        }
        return false;
    }
    static void Copy(string sourceDir, string targetDir)
    {
        try
        {
            if (!Directory.Exists(targetDir))
            {
                //Console.Clear();
                Directory.CreateDirectory(targetDir);
                PrintDirectory(targetDir);
            }

            string[] files = Directory.GetFiles(sourceDir);
            foreach (string rawFile in files)
            {
                var file = rawFile;
                bool skip = Parse(ref file);
                if (skip) continue;
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
                PrintTransfer(fileName);
            }

            string[] subDirectories = Directory.GetDirectories(sourceDir);
            foreach (string rawSubDir in subDirectories)
            {
                var subDir = rawSubDir;
                bool skip = Parse(ref subDir);
                if (skip) continue;
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(targetDir, subDirName);
                Copy(subDir, destSubDir);
            }
        }
        catch (Exception ex)
        {
            PrintError($"Transfering failed: {ex.Message}");
            throw ex;
        }
    }
    //private static void Copy(string src, string dest)
    //{
    //    var balls = Directory.EnumerateDirectories(src).ToList();
    //    var balls2 = Directory.EnumerateFiles(src).ToList();
    //    RoboCommand backup = new RoboCommand();
    //    // events
    //    //backup.OnFileProcessed += backup_OnFileProcessed;
    //    //backup.OnCommandCompleted += backup_OnCommandCompleted;

    //    // copy options
    //    backup.CopyOptions.Source = src;
    //    backup.CopyOptions.Destination = dest;
    //    //backup.CopyOptions.CopySubdirectories = true;
    //    //backup.CopyOptions.UseUnbufferedIo = true;
    //    backup.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
    //    backup.CopyOptions.CopyAll = true;
    //    backup.LoggingOptions.VerboseOutput = true;
    //    backup.OnError += (o,e) => { PrintError($"E:{e.Error}\n{e.ErrorDescription}"); };
    //    backup.OnCommandError += (o,e) => { PrintError($"CE:{e.Error}\n{e.Exception.Message}"); };

    //    backup.OnCommandCompleted += (o,e) => { PrintError($"CC:{e.StartTime}\n{e.EndTime}"); };
    //    // select options
    //    //backup.SelectionOptions.OnlyCopyArchiveFilesAndResetArchiveFlag = true;
    //    //backup.SelectionOptions.ExcludedFiles.Add("myfile.txt");
    //    //backup.SelectionOptions.ExcludedFiles.Add("my file.txt");
    //    // or
    //    // var FilestoExclude = new List<string>(new[] { "myfile.txt", "my file.txt" });
    //    // backup.SelectionOptions.ExcludedFiles.AddRange(FilestoExclude);
    //    // same methods can be used for ExcludedDirectories

    //    // retry options
    //    backup.RetryOptions.RetryCount = 1;
    //    backup.RetryOptions.RetryWaitTime = 2;
    //    backup.Start();
    //}
    public static string Download()
    {

        int attempts = 0;
        var BackupName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        var path = @$"{BasePath}\{BackupName}";
        while (Directory.Exists(path))
        {
            attempts++;
            path = @$"{BasePath}\{BackupName} ({attempts})";
        }
        Directory.CreateDirectory(path);
        Copy(@$"{DriveLetter}:\Server", path);
        return path;
    }
    public static string QuickDownload()
    {
        var backups = Directory.EnumerateDirectories(BasePath).ToList();
        backups.Sort();
        var backup = backups.FirstOrDefault();
        if (backup == null)
        {
            PrintError($"No backups were found in: \"{BasePath}\"");
            return null;
        }

        var path = @$"{backup}\world";

        Copy(@$"{DriveLetter}:\Server\world", path);
        return path;
    }
    public static void Upload(string path)
    {
        if (!Directory.Exists(path))
        {
            PrintError($"path:\"{path}\" Does not exist!");
            PrintError("Run with arguments \"-upload\" to try again");
            return;
        }
        Copy(path, @$"{DriveLetter}:\Server");
    }
    public static void QuickUpload(string path)
    {
        if (!Directory.Exists(path))
        {
            PrintError($"path:\"{path}\" Does not exist!");
            PrintError("Run with arguments \"-quickupload\" to try again");
            return;
        }
        Copy(path, @$"{DriveLetter}:\Server\world");
    }
}
public static class StringExtentions 
{
    public static int ToInt(this string input)
    {
        var success = int.TryParse(input, out int result);
        return success ? result : -1;
    }
}