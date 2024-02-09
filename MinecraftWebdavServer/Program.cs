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
        //setting default argument
        if (args.Length != 1)
            args = new string[] { "-Run" };

        //making base path if it doest exist yet
        if (!Directory.Exists(BasePath)) 
        {
            Directory.CreateDirectory(BasePath);
            Print($"Directory \"{BasePath}\" created, all servers will be stored there!", ConsoleColor.Cyan);
        }

        //letting user know what option the program is running in and if they want to change it
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

        ConsoleColor closeColor = ConsoleColor.Green;
        var val = args[0].ToLower();


        try
        {
            switch (val)
            {
                case "-run":
                    Run();
                    break;
                case "-download":
                    Download();
                    break;
                case "-quickdownload":
                    QuickDownload();
                    break;
                case "-upload":
                case "-quickupload":
                    {
                        var backup = GetLatestBackup();
                        if (backup == null)
                        {
                            PrintError($"No backups were found in: \"{BasePath}\"");
                            return;
                        }
                        if (val == "-upload")
                            Upload(backup);
                        else if (val == "-quickupload")
                            QuickUpload(backup);

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
            //if something went wrong somewhere, then log the error nicely
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var line in ex.ToString().Split('\n'))
            {
                Console.WriteLine(line);
            }
            Console.ResetColor();
            closeColor = ConsoleColor.Red;
        }
        Console.WriteLine();
        Print("Program closed.",closeColor);
        Console.WriteLine("Press any key to close...");
        Console.ReadKey();
    }
    /// <summary>
    /// Gets the latest backup
    /// </summary>
    /// <returns>string filepath or null when no backups found</returns>
    private static string? GetLatestBackup() 
    {
        var backups = Directory.EnumerateDirectories(BasePath).ToList();
        backups.Sort();
        backups.Reverse();
        var backup = backups.FirstOrDefault();
        return backup;
    }
    /// <summary>
    /// determines if the latest backup is recent enough
    /// </summary>
    /// <param name="RecentEnough">how recent the backup must be</param>
    /// <returns>bool if its recent (within range of <ref name="RecentEnough">RecentEnough</ref>)</returns>
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
    /// <summary>
    /// Print out all possible options.
    /// </summary>
    private static void ListOptions() 
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Usage:");
        Console.WriteLine("MinecraftWebdavServer -Run");
        Console.WriteLine("MinecraftWebdavServer -Download");
        Console.WriteLine("MinecraftWebdavServer -Upload");
        Console.WriteLine("MinecraftWebdavServer -QuickDownload");
        Console.WriteLine("MinecraftWebdavServer -QuickUpload");
        Console.WriteLine();
        Console.ResetColor();
    }
    /// <summary>
    /// Console.ForegroundColor = color;
    /// Console.WriteLine(msg);
    /// Console.ResetColor();
    /// 
    /// prints message with color and resets afterwards
    /// 
    /// </summary>
    /// <param name="msg">the message to print</param>
    /// <param name="color">the color to print in</param>
    private static void Print(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
    /// <summary>
    /// Print an message but with always red text prefex by "Error: "
    /// </summary>
    /// <param name="error">The error message</param>
    private static void PrintError(string error) => Print($"Error: {error}", ConsoleColor.Red);

    /// <summary>
    /// The main run method with some extra shits and giggles
    /// 1. Downloads the server.
    /// 2. Runs the server.
    /// 3. Stops the server.
    /// 4. Uploads the server.
    /// </summary>
    public static void Run() 
    {
        string path = string.Empty;
        bool backupIsRecentEnough = BackupIsRecentEnough(StartupRecentEnough);
        RecentCheck:
        var WASnt = backupIsRecentEnough ? "WAS" : "was NOT";
        var WILLnt = backupIsRecentEnough ? "will NOT" : "WILL";
        Console.WriteLine();
        Print($"The backup {WASnt} recent enough, so retriving the files {WILLnt} take long to download. Do you agree with this? (Y)es/(N)o/(S)kip", ConsoleColor.Cyan);

        var opt = Console.ReadLine();
        if (opt.ToLower() != "y" && opt.ToLower() != string.Empty)
        {
            if (opt.ToLower() == "s")
            {
                Print("!Skipped Downloading!",ConsoleColor.DarkYellow);
                var backup = GetLatestBackup();
                if (backup == null)
                {
                    PrintError($"No backups were found in: \"{BasePath}\"");
                    return;
                }
                else
                {
                    path = backup;
                }
                goto SkippedDownloading;
            }
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
        SkippedDownloading:

        var batFile = @$"{path}\start_server.bat";

        if (!File.Exists(batFile))
        {
            PrintError($"Start bat with path:\"{batFile}\" not found!");
            return;
        }
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = batFile;
        psi.WorkingDirectory = path;
        var result = Process.Start(psi);
        result.WaitForExit();

        bool uploadBackupIsRecentEnough = BackupIsRecentEnough(ClosingRecentEnough);
        UploadRecentCheck:
        var uWASnt = uploadBackupIsRecentEnough ? "WAS" : "was NOT";
        var uWILLnt = uploadBackupIsRecentEnough ? "will NOT" : "WILL";
        Console.WriteLine();
        Print($"The backup {uWASnt} recent enough, so saving the files {uWILLnt} take long to upload. Do you agree with this? (Y)es/(N)o/(S)kip", ConsoleColor.Cyan);

        var uploadOpt = Console.ReadLine();
        if (uploadOpt.ToLower() != "y" && uploadOpt.ToLower() != string.Empty)
        {
            if (opt.ToLower() == "s")
            {
                Print("!Skipped Uploading!", ConsoleColor.DarkYellow);
                return;
            }
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

    /// <summary>
    /// Parse a directory because the webdav returns them badly
    /// </summary>
    /// <param name="input">the file to potentually parse</param>
    /// <returns>a bool weather it should be completely discarded</returns>
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
    /// <summary>
    /// Copy a directory and all its subdirectories to somewhere else
    /// </summary>
    /// <param name="sourceDir">the source directory</param>
    /// <param name="targetDir">the target directory</param>
    static void Copy(string sourceDir, string targetDir)
    {
        try
        {
            if (!Directory.Exists(targetDir))
            {
                //Console.Clear();
                Directory.CreateDirectory(targetDir);
                Print($"Directory Created: {targetDir}", ConsoleColor.Cyan);
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
                Print($"Transfer Complete: {fileName}", ConsoleColor.Green);
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
    /// <summary>
    /// Downloads the server.
    /// </summary>
    /// <returns>The path that the server was downloaded in</returns>
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
    /// <summary>
    /// Quick downloads the server, by only downloading the world folder
    /// (the folder thats most likely to have any mentionable changes at all)
    /// </summary>
    /// <returns>The path that the server was downloaded in</returns>
    public static string QuickDownload()
    {
        //var backups = Directory.EnumerateDirectories(BasePath).ToList();
        //backups.Sort();
        //var backup = backups.FirstOrDefault();
        var backup = GetLatestBackup();
        if (backup == null)
        {
            PrintError($"No backups were found in: \"{BasePath}\"");
            return null;
        }

        var path = @$"{backup}\world";

        Copy(@$"{DriveLetter}:\Server\world", path);
        return backup;
    }
    /// <summary>
    /// Uploads the server.
    /// </summary>
    /// <param name="path">The path were to upload the server to.</param>
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
    /// <summary>
    /// Quickly uploads the server, by only uploading the world folder
    /// (the folder thats most likely to have any mentionable changes at all)
    /// </summary>
    /// <param name="rawPath">The path where to upload the world folder to.</param>
    public static void QuickUpload(string rawPath)
    {
        var path = @$"{rawPath}\world";
        if (!Directory.Exists(path))
        {
            PrintError($"path:\"{path}\" Does not exist!");
            PrintError("Run with arguments \"-quickupload\" to try again");
            return;
        }
        Copy(path, @$"{DriveLetter}:\Server\world");
    }
}
/// <summary>
/// A simple static class for extending string
/// </summary>
public static class StringExtentions 
{
    /// <summary>
    /// a nice and quick way to easily convert a string to an interger.
    /// </summary>
    /// <param name="input">the string to convert</param>
    /// <returns>the converted int if it was successful or -1 if it wasnt</returns>
    public static int ToInt(this string input)
    {
        var success = int.TryParse(input, out int result);
        return success ? result : -1;
    }
}