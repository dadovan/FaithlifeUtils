using System;
using System.IO;
using System.Text.Json;

namespace FaithlifeUtils;

/// <summary>
/// Holds all input configuration details for the app
/// </summary>
public class Configuration
{
    private static readonly string ConfigFileName = "config.json";

    /// <summary>
    /// Constructs an instance of the <see cref="Configuration" /> class.
    /// NOTE: this is an INTERNAL method intended to be used only during app init
    /// See <see cref="Load" /> to load a populated instance.
    /// </summary>
    /// <param name="logosId">The LogosId (from UserManager.db) for the user to run as</param>
    /// <param name="userFolder">The UserFolder (UserId from UserManager.db) for the user</param>
    /// <param name="outputFolder">The output folder to write exports into</param>
    /// <param name="notebookName">The name of the notebook to export</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameters are unexpectedly null</exception>
    public Configuration(int logosId, string userFolder, string outputFolder, string notebookName)
    {
        LogosId = logosId;
        UserFolder = userFolder ?? throw new ArgumentNullException(userFolder);
        OutputFolder = outputFolder ?? throw new ArgumentNullException(outputFolder);
        NotebookName = notebookName ?? throw new ArgumentNullException(notebookName);
    }

    /// <summary>
    /// The LogosId (from UserManager.db) for the user to run as
    /// </summary>
    public int LogosId { get; }

    /// <summary>
    /// The UserFolder (UserId from UserManager.db) for the user
    /// </summary>
    public string UserFolder { get; }

    /// <summary>
    /// The output folder to write exports into
    /// </summary>
    public string OutputFolder { get; }

    /// <summary>
    /// The name of the notebook to export
    /// </summary>
    public string NotebookName { get; }

    /// <summary>
    /// Loads in the config values from the configuration file
    /// </summary>
    /// <returns>A populated <see cref="Configuration" /> instance</returns>
    /// <exception cref="ArgumentException">Thrown if deserialization fails to produce a valid instance</exception>
    public static Configuration Load()
    {
        var config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(ConfigFileName));
        if (config == null)
            throw new ArgumentException($"Unable to deserialize the {ConfigFileName} file");
        return config;
    }
}