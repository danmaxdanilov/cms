using Microsoft.Extensions.Options;

namespace CMS.Agent.Repositories;

public interface IFileRepository
{
    Task<string> CopyNewEntryAsync(string source, string destination);

    void InitDirectories();
    void ShutDownDirectories();
}

public class FileRepository : IFileRepository
{
    private readonly string _rootDir;
    private readonly string _importDir;
    public FileRepository(IOptions<AgentSettings> settings)
    {
        _rootDir = settings.Value.RootDir;
        _importDir = settings.Value.ImportDir;
        
        InitDirectories();
    }

    public async Task<string> CopyNewEntryAsync(string source, string destination)
    {
        var importSource = Path.Combine(_importDir, source);
        if (string.IsNullOrEmpty(importSource) && Path.IsPathFullyQualified(importSource))
            throw new ArgumentException("Can't be empty string and should be UNC path string", nameof(source));

        var repositoryDestination = Path.Combine(_rootDir, destination);
        if (string.IsNullOrEmpty(repositoryDestination) && Path.IsPathFullyQualified(repositoryDestination))
            throw new ArgumentException("Can't be empty string and should be UNC path string", nameof(source));
        
        var destinationDir = Path.GetDirectoryName(repositoryDestination);
        if (Directory.Exists(destinationDir))
            throw new FileLoadException(string.Format("Output directory already exists \"{0}\"", destinationDir));
        if (File.Exists(repositoryDestination))
            throw new FileLoadException(string.Format("Output file already exists \"{0}\"", Path.GetFileName(destinationDir)));
        
        Directory.CreateDirectory(destinationDir);
        await CopyFileAsync(importSource, repositoryDestination);
        return repositoryDestination;
    }
    
    private async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        using (Stream source = File.Open(sourcePath, FileMode.Create))
        {
            using(Stream destination = File.Create(destinationPath))
            {
                await source.CopyToAsync(destination);
            }
        }
    }
    
    public void InitDirectories()
    {
        if (!Directory.Exists(_rootDir))
            Directory.CreateDirectory(_rootDir);
        if (!Directory.Exists(_importDir))
            Directory.CreateDirectory(_importDir);
    }
    
    public void ShutDownDirectories()
    {
        if (Directory.Exists(_rootDir))
            Directory.Delete(_rootDir,true);
        if (Directory.Exists(_importDir))
            Directory.Delete(_importDir, true);
    }
}