using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace VideoContactSheetMaker
{
    static class Utils
    {
        public const string FFprobeExecutableName = "ffprobe";
        public const string FFmpegExecutableName = "ffmpeg";
        public static string FfmpegPath { get; }
        public static string FfprobePath { get; }
        public static readonly string CurrentDirectory;
        private static readonly string[] suf = { " B", " KB", " MB", " GB", " TB", " PB", " EB" };

        public static bool FfFilesExist
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return File.Exists(FfmpegPath + ".exe") && File.Exists(FfprobePath + ".exe");
                }
                return File.Exists(FfmpegPath) && File.Exists(FfprobePath);
            }
        }

        static Utils()
        {
            CurrentDirectory = Path.GetDirectoryName(typeof(FFmpegWrapper.FFmpegWrapper).Assembly.Location);
            var pathsEnv = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (File.Exists(CurrentDirectory + $"\\{FFmpegExecutableName}.exe"))
                    FfmpegPath = CurrentDirectory + $"\\{FFmpegExecutableName}";
                else if (File.Exists(CurrentDirectory + $"\\bin\\{FFmpegExecutableName}.exe"))
                    FfmpegPath = CurrentDirectory + $"\\bin\\{FFmpegExecutableName}";
                if (File.Exists(CurrentDirectory + $"\\{FFprobeExecutableName}.exe"))
                    FfprobePath = CurrentDirectory + $"\\{FFprobeExecutableName}";
                else if (File.Exists(CurrentDirectory + $"\\bin\\{FFprobeExecutableName}.exe"))
                    FfprobePath = CurrentDirectory + $"\\bin\\{FFprobeExecutableName}";
            }


            if (pathsEnv != null && (string.IsNullOrEmpty(FfprobePath) || string.IsNullOrEmpty(FfmpegPath)))
                foreach (var path in pathsEnv)
                {
                    if (!Directory.Exists(path))
                    {
                        continue;
                    }
                    try
                    {
                        var files = new DirectoryInfo(path).GetFiles();

                        if (string.IsNullOrEmpty(FfmpegPath))
                            FfmpegPath = files.FirstOrDefault(x => x.Name.StartsWith(FFmpegExecutableName, true, CultureInfo.InvariantCulture))
                                ?.FullName;
                        if (string.IsNullOrEmpty(FfprobePath))
                            FfprobePath = files.FirstOrDefault(x => x.Name.StartsWith(FFprobeExecutableName, true, CultureInfo.InvariantCulture))
                                ?.FullName;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }
        }


        /// <summary>
        /// Trims milliseconds from a timespan making it better compare-able against another timespan
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static TimeSpan TrimMiliseconds(this TimeSpan ts) => new TimeSpan(ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
        /// <summary>
        /// Formats byte length to a human readable format
        /// </summary>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static string BytesToString(long byteCount)
        {
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
        }


        /// <summary>
        /// Get safe path on all systems ignoring slashes
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static string SafePathCombine(string path1, string path2)
        {
            if (!Path.IsPathRooted(path2))
                Path.Combine(path1, path2);

            path2 = path2.TrimStart(Path.DirectorySeparatorChar);
            path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);

            return Path.Combine(path1, path2);
        }
    }
}
