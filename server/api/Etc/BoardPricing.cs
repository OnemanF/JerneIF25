namespace api.Etc;

public static class BoardPricing
{
    public static decimal PriceForCount(int count) => count switch
    {
        5 => 20m,
        6 => 40m,
        7 => 80m,
        8 => 160m,
        _ => throw new ArgumentOutOfRangeException(nameof(count), "Numbers must be 5–8.")
    };

    public static void EnsureValidNumbers(int[] numbers)
    {
        if (numbers is null || numbers.Length < 5 || numbers.Length > 8)
            throw new ArgumentException("Numbers must contain 5–8 entries.");
        if (numbers.Any(n => n < 1 || n > 16))
            throw new ArgumentException("Numbers must be between 1 and 16.");
        if (numbers.Distinct().Count() != numbers.Length)
            throw new ArgumentException("Numbers must be unique.");
    }
}