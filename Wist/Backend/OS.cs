using static System.PlatformID;

namespace Wist.Backend;

// ReSharper disable once InconsistentNaming
public static class OS
{
    static OS()
    {
        if (!IsLinux() && !IsWindows())
            throw new InvalidOperationException("Unsupported operating system");
    }

    public static bool IsLinux()
    {
        return Environment.OSVersion.Platform == Unix;
    }

    public static bool IsWindows()
    {
        return Environment.OSVersion.Platform is Win32S or Win32Windows or Win32NT or WinCE;
    }
}