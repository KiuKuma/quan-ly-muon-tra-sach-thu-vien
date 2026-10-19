using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

public sealed class LibraryFileStore(IWebHostEnvironment environment, IConfiguration configuration)
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string[] FileNames =
    [
        "users.txt",
        "profiles.txt",
        "readers.txt",
        "categories.txt",
        "books.txt",
        "staff.txt",
        "loans.txt",
        "loan-details.txt",
        "returns.txt",
        "return-details.txt"
    ];

    private readonly string _directoryPath = ResolvePath(environment.ContentRootPath,
        configuration["LibraryData:DirectoryPath"] ?? "App_Data");
    private string LegacyFilePath => Path.Combine(_directoryPath, "library.json");

    public async Task<T> ReadAsync<T>(Func<LibraryData, T> selector)
    {
        await FileLock.WaitAsync();
        try
        {
            var data = await LoadUnsafeAsync();
            return selector(data);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<T> UpdateAsync<T>(Func<LibraryData, T> update)
    {
        await FileLock.WaitAsync();
        try
        {
            var data = await LoadUnsafeAsync();
            var result = update(data);
            await SaveUnsafeAsync(data);
            return result;
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task EnsureCreatedAsync()
    {
        await FileLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_directoryPath);
            var allFilesExist = FileNames.All(name => File.Exists(Path.Combine(_directoryPath, name)));
            if (allFilesExist)
            {
                await BackupLegacyFileAsync();
                return;
            }

            if (File.Exists(LegacyFilePath))
            {
                await using var stream = File.OpenRead(LegacyFilePath);
                var legacyData = await JsonSerializer.DeserializeAsync<LibraryData>(stream, JsonOptions)
                                 ?? new LibraryData();
                await SaveUnsafeAsync(legacyData);
                await BackupLegacyFileAsync();
                return;
            }

            await SaveUnsafeAsync(await LoadUnsafeAsync());
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task BackupLegacyFileAsync()
    {
        if (!File.Exists(LegacyFilePath))
        {
            return;
        }

        var backupPath = LegacyFilePath + ".migrated.bak";
        if (!File.Exists(backupPath))
        {
            File.Copy(LegacyFilePath, backupPath);
        }

        try
        {
            File.Delete(LegacyFilePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        await Task.CompletedTask;
    }

    private async Task<LibraryData> LoadUnsafeAsync()
    {
        return new LibraryData
        {
            Accounts = await ReadLinesAsync<LibraryAccount>("users.txt"),
            Profiles = await ReadLinesAsync<LibraryUserProfile>("profiles.txt"),
            DocGia = await ReadLinesAsync<Models.DocGia>("readers.txt"),
            TheLoai = await ReadLinesAsync<Models.TheLoai>("categories.txt"),
            Sach = await ReadLinesAsync<Models.Sach>("books.txt"),
            NhanVien = await ReadLinesAsync<Models.NhanVien>("staff.txt"),
            PhieuMuon = await ReadLinesAsync<Models.PhieuMuon>("loans.txt"),
            ChiTietPhieuMuon = await ReadLinesAsync<Models.ChiTietPhieuMuon>("loan-details.txt"),
            PhieuTra = await ReadLinesAsync<Models.PhieuTra>("returns.txt"),
            ChiTietPhieuTra = await ReadLinesAsync<Models.ChiTietPhieuTra>("return-details.txt")
        };
    }

    private async Task SaveUnsafeAsync(LibraryData data)
    {
        Directory.CreateDirectory(_directoryPath);
        var files = new Dictionary<string, IEnumerable<string>>
        {
            ["users.txt"] = SerializeLines(data.Accounts),
            ["profiles.txt"] = SerializeLines(data.Profiles),
            ["readers.txt"] = SerializeLines(data.DocGia),
            ["categories.txt"] = SerializeLines(data.TheLoai),
            ["books.txt"] = SerializeLines(data.Sach),
            ["staff.txt"] = SerializeLines(data.NhanVien),
            ["loans.txt"] = SerializeLines(data.PhieuMuon),
            ["loan-details.txt"] = SerializeLines(data.ChiTietPhieuMuon),
            ["returns.txt"] = SerializeLines(data.PhieuTra),
            ["return-details.txt"] = SerializeLines(data.ChiTietPhieuTra)
        };

        var temporaryPaths = new List<string>();
        try
        {
            foreach (var (fileName, lines) in files)
            {
                var temporaryPath = Path.Combine(_directoryPath, fileName + ".tmp");
                temporaryPaths.Add(temporaryPath);
                await File.WriteAllLinesAsync(temporaryPath, lines, new UTF8Encoding(false));
            }

            foreach (var fileName in FileNames)
            {
                File.Move(Path.Combine(_directoryPath, fileName + ".tmp"),
                    Path.Combine(_directoryPath, fileName), overwrite: true);
            }
        }
        catch
        {
            foreach (var temporaryPath in temporaryPaths)
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            throw;
        }
    }

    private async Task<List<T>> ReadLinesAsync<T>(string fileName)
    {
        var path = Path.Combine(_directoryPath, fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        var records = new List<T>();
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        for (var index = 0; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                continue;
            }

            try
            {
                var record = JsonSerializer.Deserialize<T>(lines[index], JsonOptions);
                if (record is not null)
                {
                    records.Add(record);
                }
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    $"Không đọc được {fileName}, dòng {index + 1}.", exception);
            }
        }

        return records;
    }

    private static IEnumerable<string> SerializeLines<T>(IEnumerable<T> records)
    {
        return records.Select(record => JsonSerializer.Serialize(record, JsonOptions));
    }

    private static string ResolvePath(string contentRoot, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRoot, configuredPath));
    }
}