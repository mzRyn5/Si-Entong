namespace Store.Api.Configuration;

public static class StartupSecurity
{
    private const string DevelopmentSecret = "development-secret-key-change-in-production-minimum-32-characters";
    private const string PlaceholderSecret = "${JWT_SECRET}";

    public static string ValidateJwtSecret(string? secret, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret == PlaceholderSecret)
        {
            throw new InvalidOperationException("JWT secret must be configured before the API starts.");
        }

        if (!isDevelopment && secret == DevelopmentSecret)
        {
            throw new InvalidOperationException("Development JWT secret cannot be used outside Development.");
        }

        if (!isDevelopment && secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters outside Development.");
        }

        return secret;
    }
}
