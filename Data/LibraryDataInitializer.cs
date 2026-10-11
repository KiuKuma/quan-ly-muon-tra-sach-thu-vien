namespace quan_ly_muon_tra_sach_thu_vien.Data;

public static class LibraryDataInitializer
{
    public static async Task InitializeAsync(
        LibraryFileStore store,
        LibrarySampleDataSeeder sampleData,
        LibraryAccountService accounts,
        IConfiguration configuration,
        ILogger logger)
    {
        await store.EnsureCreatedAsync();
        await sampleData.SeedAsync();

        var initialAdministratorEmail = configuration["LibraryAdmin:InitialAdministratorEmail"];
        if (!string.IsNullOrWhiteSpace(initialAdministratorEmail))
        {
            await accounts.EnsureInitialAdministratorAsync(initialAdministratorEmail);
        }

        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Kho JSON đã sẵn sàng. Chưa cấu hình tài khoản Thủ thư.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Cần cấu hình đồng thời BootstrapAdmin:Email và BootstrapAdmin:Password.");
        }

        await accounts.EnsureLibrarianAsync(email, password);
        logger.LogInformation("Đã sẵn sàng tài khoản Thủ thư được cấu hình.");
    }
}