using System.IO;
using System.Linq;

namespace FaithlifeUtils;

/// <summary>
/// Holds extension methods for the <see cref="Path"/> class
/// </summary>
public static class PathEx
{
    /// <summary>
    /// Cleanses the provided <paramref name="fileName"/>, replacing invalid file name characters with an underscore.
    /// </summary>
    /// <param name="fileName">The name of the file to cleanse</param>
    /// <returns>A cleansed version of <paramref name="fileName"/></returns>
    public static string CleanseFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>
    /// Cleanses the provided <paramref name="path"/>, replacing invalid path characters with an underscore.
    /// </summary>
    /// <param name="path">The path to cleanse</param>
    /// <returns>A cleansed version of <paramref name="path"/></returns>
    public static string CleansePath(string path)
    {
        var invalidChars = Path.GetInvalidPathChars().ToHashSet();
        return new string(path.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>
    /// Cleanses the provided <paramref name="path"/> and <paramref name="fileName"/>, replacing characters with an underscore.
    /// </summary>
    /// <param name="path">The path to cleanse</param>
    /// <param name="fileName">The name of the file to cleanse</param>
    /// <returns>A cleansed version of <paramref name="path"/> and <paramref name="fileName"/></returns>
    public static (string, string) CleansePathAndFileName(string path, string fileName)
    {
        return (CleansePath(path), CleanseFileName(fileName));
    }
}
