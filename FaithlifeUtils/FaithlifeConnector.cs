using Faithlife.NotesApi.v1;
using Libronix.DigitalLibrary;
using Libronix.DigitalLibrary.NotesTool;
using Libronix.DigitalLibrary.ResourceAudit;
using Libronix.DigitalLibrary.Resources;
using Libronix.DigitalLibrary.Resources.Logos;
using Libronix.DigitalLibrary.Utility;
using Libronix.DigitalLibrary.Utility.NotesTool;
using Libronix.DigitalLibrary.WebCache;
using Libronix.Utility.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

// Disable warnings when passing interpolated strings to Serilog
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace FaithlifeUtils;

public sealed class FaithlifeConnector : IDisposable
{
    private static int _singletonCounter;
    private static readonly string RootPath = FindRootPath();
    private readonly LibraryCatalog _libraryCatalog;
    private readonly LicenseManager _licenseManager;
    private readonly ILogger _log;
    private readonly NotesToolManager _notesToolManager;

    private readonly Dictionary<string, Resource> _resourceCache = new();
    private readonly ResourceManager _resourceManager;

    private FaithlifeConnector(ILogger log, LibraryCatalog libraryCatalog, LicenseManager licenseManager, NotesToolManager notesToolManager, ResourceManager resourceManager)
    {
        _log = log;
        _notesToolManager = notesToolManager;
        _libraryCatalog = libraryCatalog;
        _licenseManager = licenseManager;
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Disposes of all resources allocated by this instance
    /// </summary>
    public void Dispose()
    {
        _log.Debug($"Disposing of the core {nameof(FaithlifeConnector)} instance");
        _notesToolManager.Dispose();
        _libraryCatalog.Dispose();
        _resourceManager.Dispose();
        _licenseManager.Dispose();
        Interlocked.Decrement(ref _singletonCounter);
        _log.Debug("Disposal complete");
    }

    /// <summary>
    /// Creates an instance of the <see cref="FaithlifeConnector" /> class
    /// </summary>
    /// <param name="logosId">The LogosId for the user</param>
    /// <param name="userFolder">The folder id for the user</param>
    /// <returns>An instance of the <see cref="FaithlifeConnector" /> class</returns>
    public static FaithlifeConnector Create(int logosId, string userFolder)
    {
        ArgumentNullOrWhiteSpaceException.ThrowIfNullOrWhiteSpace(userFolder);
        if (Interlocked.Increment(ref _singletonCounter) > 1)
            throw new LockRecursionException($"{nameof(FaithlifeConnector)} is a singleton and an instance has already been created.");

        var log = Log.ForContext<FaithlifeConnector>();
        log.Debug($"Creating the core {nameof(FaithlifeConnector)} instance");

        var rootPath = RootPath;
        var dataPath = Path.Combine(rootPath, "Data", userFolder);
        var dataKeyLinkManagerPath = Path.Combine(dataPath, "KeyLinkManager");
        var dataLicenseManagerPath = Path.Combine(dataPath, "LicenseManager");
        var dataLibraryCatalogPath = Path.Combine(dataPath, "LibraryCatalog");
        var dataResourceManagerPath = Path.Combine(dataPath, "ResourceManager");
        var dataReverseInterlinearManagerPath = Path.Combine(dataPath, "ReverseInterlinearManager");
        var dataWebCachePath = Path.Combine(dataPath, "WebCache");
        var documentsUserPath = Path.Combine(rootPath, "Documents", userFolder);
        var documentsUserResourceCollectionManagerPath = Path.Combine(documentsUserPath, "ResourceCollectionManager");
        var sharedPath = Path.Combine(rootPath, "Shared");
        var sharedProductsPath = Path.Combine(sharedPath, "Products");
        UpdatePath();

        // A lot of inspiration came from Libronix.DigitalLibrary.Utility.DigitalLibraryServices.Create*
        LibraryCatalog? libraryCatalog = null;
        LicenseManager? licenseManager = null;
        NotesToolManager? notesToolManager = null;
        ResourceManager? resourceManager = null;
        try
        {
            log.Debug("Creating the LicenseManager");
            licenseManager = new LicenseManager(logosId.ToString(), dataLicenseManagerPath, sharedProductsPath);

            log.Debug("Creating the ResourceManager");
            LogosResourceDriver.Initialize();
            var documentsConnectorProviderFactory = ConnectorProviderFactory.CreateLocalFileDatabaseFactory(documentsUserPath);
            var webCacheManager = new WebCacheManager(dataWebCachePath, new StandardWebCacheManagerPolicy(null, null));
            var services = new ResourceServices(new DataTypeOptions(), webCacheManager);
            resourceManager = new ResourceManager(dataResourceManagerPath, documentsConnectorProviderFactory, licenseManager, services);

            log.Debug("Creating the LibraryCatalog");
            var libraryCatalogSettings = new LibraryCatalogSettings
            {
                LibraryCatalogFolder = dataLibraryCatalogPath,
                DocumentsConnectorProviderFactory = documentsConnectorProviderFactory,
                ServiceEndpointProvider = new ServiceEndpointProvider(),
                ResourceAuditLogger = new DisabledResourceAuditLogger()
            };
            libraryCatalog = new LibraryCatalog(libraryCatalogSettings);

            log.Debug("Creating the NotesToolManager");
            var reverseInterlinearManager = new ReverseInterlinearManager(dataReverseInterlinearManagerPath, resourceManager, libraryCatalog);
            var resourceCollectionManager = new ResourceCollectionManager(documentsUserResourceCollectionManagerPath, libraryCatalog);
            var keyLinkManager = new EnhancedKeyLinkManager(dataKeyLinkManagerPath, documentsConnectorProviderFactory, resourceManager, resourceCollectionManager, libraryCatalog, 0);
            var resourceLists = new ResourceLists(resourceManager, libraryCatalog, keyLinkManager, reverseInterlinearManager);
            var dataTypeLists = new DataTypeLists(resourceLists);
            var notesDigitalLibraryService = new NotesDigitalLibraryService(resourceManager, reverseInterlinearManager, libraryCatalog, dataTypeLists, null);

            Dictionary<string, string> GetResourceLabels(IReadOnlyList<string> resourceIds)
            {
                return libraryCatalog.GetResourceInfos(resourceIds, new ResourceFieldSet(ResourceField.AbbreviatedTitle, ResourceField.Title))
                    .ToDictionary<ResourceInfo, string, string>(ri => ri.ResourceId, ri => String.IsNullOrEmpty(ri.AbbreviatedTitle) ? ri.Title : ri.AbbreviatedTitle);
            }

            notesToolManager = new NotesToolManager(new NotesToolManagerSettings
            {
                UserId = logosId,
                ConnectorProviderFactory = documentsConnectorProviderFactory,
                GetResourceLabels = GetResourceLabels,
                NotesDigitalLibraryService = notesDigitalLibraryService
            });

            log.Debug($"Finished creating the core {nameof(FaithlifeConnector)} instance");
            return new FaithlifeConnector(log, libraryCatalog, licenseManager, notesToolManager, resourceManager);
        }
        catch (Exception e)
        {
            log.Error($"{e.GetType().Name}: {e.Message}");
            notesToolManager?.Dispose();
            libraryCatalog?.Dispose();
            resourceManager?.Dispose();
            licenseManager?.Dispose();
            Interlocked.Decrement(ref _singletonCounter);
            log.Debug("Cleanup complete");
            throw;
        }
    }

    /// <summary>
    /// Opens the given resource with caching.
    /// </summary>
    /// <param name="resourceId">The resource to open.</param>
    /// <returns>The <see cref="Resource" /></returns>
    public Resource OpenResource(string resourceId)
    {
        ArgumentNullOrWhiteSpaceException.ThrowIfNullOrWhiteSpace(resourceId);

        if (_resourceCache.TryGetValue(resourceId, out var resource))
        {
            _log.Debug($"Opening resource {resourceId} from cache");
            return resource;
        }

        _log.Debug($"Opening uncached resource {resourceId}");
        resource = _resourceManager.OpenResource(resourceId);
        _resourceCache[resourceId] = resource;
        return resource;
    }

    /// <summary>
    /// Gets the specified notebook
    /// </summary>
    /// <param name="title">The title of the notebook to retrieve</param>
    /// <returns>The desired notebooks</returns>
    /// <exception cref="DataException">Thrown if the find action hits an unexpected issue</exception>
    public NotebookDto GetNotebook(string title)
    {
        ArgumentNullOrWhiteSpaceException.ThrowIfNullOrWhiteSpace(title);
        var notebooks = GetNotebooks();
        var notebook = notebooks.Single(n => n.Title?.EqualsI(title) ?? false);
        _log.Debug($"Retrieved notebook {notebook.Id}");
        return notebook;
    }

    /// <summary>
    /// Gets a read-only list of notebooks
    /// </summary>
    /// <returns>A read-only list of notebooks</returns>
    /// <exception cref="DataException">Thrown if the find action hits an unexpected issue</exception>
    public IEnumerable<NotebookDto> GetNotebooks()
    {
        var request = new FindNotebooksRequestDto
        {
            NotebookFields = new[] { NotebookField.Id, NotebookField.Title, NotebookField.NoteCount, NotebookField.Created, NotebookField.Modified }
        };
        var response = _notesToolManager.ClientNotesApi.FindNotebooksAsync(request, CancellationToken.None).Result.Value;
        if (response.Notebooks == null)
            throw new DataException("Encountered an unexpected null when querying for notebooks");
        var notebooks = new ReadOnlyCollection<NotebookDto>(response.Notebooks.ToArray());
        _log.Debug($"Retrieved {notebooks.Count} notes.");
        return notebooks;
    }

    /// <summary>
    /// Gets a read-only list of notes, optionally filtering by notebook
    /// </summary>
    /// <param name="notebookId">An optional ExternalId of the notebook for filtering</param>
    /// <returns>A read-only list of notes passing the optional filter</returns>
    /// <exception cref="DataException">Thrown if the find action hits an unexpected issue</exception>
    public IEnumerable<NoteDto> GetNotes(string? notebookId = null)
    {
        var request = new FindNotesRequestDto
        {
            NoteFields = new[] { NoteField.Id, NoteField.CreatedBy, NoteField.Content, NoteField.Anchors, NoteField.Clipping, NoteField.Placement, NoteField.Tags, NoteField.Created },

            AnchorSettings = new NoteAnchorSettingsDto
            {
                TextRange = new NoteAnchorTextRangeSettingsDto
                {
                    ReferenceField = true,
                    TargetFields = true
                }
            }
        };
        var response = _notesToolManager.ClientNotesApi.FindNotesAsync(request, CancellationToken.None).Result.Value;
        if (response.Notes == null)
            throw new DataException("Encountered an unexpected null when querying for notes");
        var notes = response.Notes
            .Where(n => String.IsNullOrWhiteSpace(notebookId) || (n.Placement?.NotebookId?.EqualsI(notebookId) ?? false))
            .ToList().AsReadOnly();
        _log.Debug($"Retrieved {notes.Count} notes.");
        return notes;
    }

    /// <summary>
    /// Updates the assembly resolver for the app domain so we can load our references directly from the Faithlife folders.
    /// This assists with keeping the code working as Logos/Verbum updates.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3885:\"Assembly.Load\" should be used", Justification = "Definitely a code smell; judging OK in this scenario.")]
    internal static void AddResolver()
    {
        Log.ForContext<FaithlifeConnector>().Debug("Adding assembly resolver");
        AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
        {
            Log.ForContext<FaithlifeConnector>().Debug($"Attempting to load assembly {eventArgs.Name}");
            var assemblyName = eventArgs.Name[..eventArgs.Name.IndexOf(",", StringComparison.InvariantCultureIgnoreCase)];
            var assemblyPath = Environment.ExpandEnvironmentVariables($@"{RootPath}\System\{assemblyName}.dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        };
    }

    /// <summary>
    /// Gets the root path of the Verbum/Logos install
    /// </summary>
    /// <returns>The root path of the install</returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static string FindRootPath()
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var verbumPath = Path.Combine(localAppDataPath, "Verbum");
        if (Directory.Exists(verbumPath))
        {
            Log.ForContext<FaithlifeConnector>().Debug($"Root path: {verbumPath}");
            return verbumPath;
        }

        var logosPath = Path.Combine(localAppDataPath, "Logos");
        if (Directory.Exists(logosPath))
        {
            Log.ForContext<FaithlifeConnector>().Debug($"Root path: {logosPath}");
            return logosPath;
        }

        throw new DirectoryNotFoundException($"Unable to find a Verbum or Logos directory under '{localAppDataPath}'");
    }

    /// <summary>
    /// Ensures the location of the Faithlife binaries are on the path so we use those
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if we are unable to retrieve the path for any reason</exception>
    private static void UpdatePath()
    {
        var currentPath = Environment.GetEnvironmentVariable("path") ?? throw new ArgumentException("Unable to get the 'path' environment variable.");
        var systemPath = Path.Combine(RootPath, "System");
        if (currentPath.Contains($"{Path.PathSeparator}{systemPath}")) // Imperfect
            return;
        var newPath = $"{currentPath}{Path.PathSeparator}{systemPath}";
        Log.ForContext<FaithlifeConnector>().Debug($"Updated path to: {newPath}");
        Environment.SetEnvironmentVariable("path", newPath);
    }
}
