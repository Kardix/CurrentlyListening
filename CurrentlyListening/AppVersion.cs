using System.Reflection;

namespace CurrentlyListening;

// The version from <Version> in the .csproj.
public static class AppVersion
{
    public static Version Current { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    // e.g. "1.1.5"
    public static string Display { get; } = Current.ToString(3);
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2026 Tom "TKoNoR"
