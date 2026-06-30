namespace Store.SharedKernel;

public static class Guard
{
    public static void AgainstEmpty(Guid? value, string message)
    {
        if (value == null || value == Guid.Empty)
            throw new ArgumentException(message);
    }

    public static void AgainstEmpty(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message);
    }

    public static void AgainstNegative(decimal value, string message)
    {
        if (value < 0)
            throw new ArgumentException(message);
    }

    public static void AgainstNegativeOrZero(decimal value, string message)
    {
        if (value <= 0)
            throw new ArgumentException(message);
    }

    public static void AgainstNegative(int value, string message)
    {
        if (value < 0)
            throw new ArgumentException(message);
    }

    public static void AgainstOutOfRange(int value, int min, int max, string message)
    {
        if (value < min || value > max)
            throw new ArgumentException(message);
    }

    public static T AgainstNull<T>(T? value, string message) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(message);
        return value;
    }
}
