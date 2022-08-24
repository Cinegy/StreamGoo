using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Char;

namespace StreamGoo.Helpers;

public static class Product
{
    private static string _tracingName;

    #region Constructors

    static Product()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var appFile = Path.Combine(AppContext.BaseDirectory, "streamgoo");
        
        if (File.Exists(appFile))
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(appFile);
        
            Version = versionInfo.FileVersion;
            BuildTime = File.GetCreationTime(appFile);
        }
        else if (File.Exists($"{appFile}.exe"))
        {
            var versionInfo = FileVersionInfo.GetVersionInfo($"{appFile}.exe");

            Version = versionInfo.FileVersion;
            BuildTime = File.GetCreationTime(appFile);
        }

        Name = "StreamGoo";
    }

    #endregion

    #region Static members

    public static string Name { get; }

    public static string TracingName
    {
        get
        {
            return _tracingName ??= string.Concat(Name.Where(c => !IsWhiteSpace(c))).ToLowerInvariant();
        }
    }
    public static string Version { get; } = "0.0";
    public static DateTime BuildTime { get; } = DateTime.MinValue;

    #endregion
}