using System;
using System.IO;
using CommandLine;
using Serilog;

namespace FaithlifeUtils;

/// <summary>
/// Holds all input configuration details for the app
/// </summary>
public class Configuration
{
    /// <summary>
    /// The LogosId (from UserManager.db) for the user to run as
    /// </summary>
    [Option(HelpText = "The LogosId (from UserManager.db) for the user to run as", Required = true)]
    public int LogosId { get; set; }

    /// <summary>
    /// The UserFolder (UserId from UserManager.db) for the user
    /// </summary>
    [Option(HelpText = "The UserFolder (UserId from UserManager.db) for the user")]
    public string UserFolder { get; set; } = null!;

    /// <summary>
    /// The output folder to write exports into
    /// </summary>
    [Option(HelpText = "The output folder to write exports into", Required = true)]
    public string OutputFolder { get; set; } = null!;

    /// <summary>
    /// The name of the notebook to export
    /// </summary>
    [Option(HelpText = "The name of the notebook to export", Required = true)]
    public string NotebookName { get; set; } = null!;

    public static Configuration Instance { get; private set; } = null!;

    /// <summary>
    /// Validates that the config is in a good state
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if validation fails</exception>
    /// <exception cref="ArgumentNullOrWhiteSpaceException">Thrown if validation fails</exception>
    public void Validate()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(Configuration)} was previously set");
        // Technically the Option(Required=true) properties above have already been validated but we'll double-check here anyway
        if (LogosId <= 1)
            throw new ArgumentException("Expected a value for LogosId of > 1");
        if (String.IsNullOrWhiteSpace(UserFolder))
        {
            var root = Path.Combine(FaithlifeConnector.FindRootPath(), "Data");
            var folders = Directory.GetDirectories(root);
            if (folders.Length != 1)
                throw new ArgumentException($"Unable to automatically determine UserFolder.  Folders found:{Environment.NewLine}{String.Join(Environment.NewLine, folders)}");
            UserFolder = Path.GetFileName(folders[0]);
            Log.ForContext<Configuration>().Debug($"No {nameof(UserFolder)} supplied.  Using '{UserFolder}' from '{folders[0]}'");
        }

        ArgumentNullOrWhiteSpaceException.ThrowIfNullOrWhiteSpace(OutputFolder);
        ArgumentNullOrWhiteSpaceException.ThrowIfNullOrWhiteSpace(NotebookName);

        Instance = this;
    }
}
