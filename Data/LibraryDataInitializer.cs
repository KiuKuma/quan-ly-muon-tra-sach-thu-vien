namespace quan_ly_muon_tra_sach_thu_vien.Data;

public static class LibraryDataInitializer
{
    public static async Task InitializeAsync(
        LibraryFileStore store,
        LibrarySampleDataSeeder sampleData,
        LibraryAccountService accounts,
        ILogger logger)
    {
        await store.EnsureCreatedAsync();
        await accounts.EnsureDefaultAccountsAsync();
        await sampleData.SeedAsync();
        logger.LogInformation("Kho dữ liệu thư viện và bốn tài khoản quản lý/nhân viên đã sẵn sàng.");
    }
}