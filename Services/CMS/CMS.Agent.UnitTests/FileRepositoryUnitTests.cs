using CMS.Agent.Repositories;
using Microsoft.Extensions.Options;

namespace CMS.Agent.UnitTests;

public class FileRepositoryUnitTests : IDisposable
{
    private readonly IOptions<AgentSettings> _fakeOptions;
    private readonly IFileRepository _repository;
    public FileRepositoryUnitTests()
    {
        var settings = new AgentSettings
        {
            RootDir = "repository",
            ImportDir = "import"
        };
        _fakeOptions = Options.Create(settings);
        _repository = new FileRepository(_fakeOptions);
    }

    [Fact]
    public async Task CopyNewEntry_WhenCalledShouldCreateDirectoryAndCopyFile()
    {
        //Arrange
        var values = SetupFiles();

        //Act
        var result = await _repository.CopyNewEntryAsync(values.Item1, values.Item2);

        //Assert
        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains(values.Item3, result);
        Assert.Contains(values.Item4, result);
    }
    
    [Fact]
    public async Task CopyNewEntry_WhenCalledShouldThrowExceptionDestDirExists()
    {
        //Arrange
        var values = SetupFiles();
        var fakeDirectory = Path.Combine("repository", values.Item4);
        Directory.CreateDirectory(fakeDirectory);
        await File.WriteAllTextAsync(Path.Combine(fakeDirectory, values.Item3), string.Empty);

        //Act & Assert
        var exception = await Assert.ThrowsAsync<FileLoadException>(async () => 
            await _repository.CopyNewEntryAsync(values.Item1, values.Item2));
        Assert.Contains(fakeDirectory, exception.Message);
    }
    
    public void Dispose()
    {
        Directory.Delete(_fakeOptions.Value.RootDir, true);
    }

    private Tuple<string, string, string, string> SetupFiles()
    {
        if (Directory.Exists(_fakeOptions.Value.RootDir))
            Directory.Delete(_fakeOptions.Value.RootDir, true);
        if (Directory.Exists(_fakeOptions.Value.ImportDir))
            Directory.Delete(_fakeOptions.Value.ImportDir, true);
        
        _repository.InitDirectories();
        
        var fakeImportFileName = "12342445.pkg";
        using (File.WriteAllTextAsync(Path.Combine("import", fakeImportFileName), string.Empty));

        var fakePackageFile = "mc.dmg";
        var fakePackageName = "mc";
        var fakePackageVersion = "1.27";
        var fakeDestFolder = string.Format("{0}@{1}", fakePackageName, fakePackageVersion);
        var fakeDestPath = Path.Combine(fakeDestFolder, fakePackageFile);

        return new Tuple<string, string, string, string>(fakeImportFileName, fakeDestPath, fakePackageFile, fakeDestFolder);
    }
}