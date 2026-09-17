// NATIVE WPF (.NET 9, C# 13) password service.
// Security: uses System.Security.Cryptography.RandomNumberGenerator (CSPRNG).
// Privacy: pure in-memory function — no file, registry, settings, or network I/O.
using System.Security.Cryptography;
using System.Text;

namespace PasswordGenerator.Services;

public sealed record PasswordOptions(
    int Length,
    bool IncludeLower,
    bool IncludeUpper,
    bool IncludeDigits,
    bool IncludeSymbols,
    bool ExcludeAmbiguous,
    bool EnsureEachCategory);

public static class PasswordGeneratorService
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?/~";

    // Characters commonly confused when reading/typing.
    private static readonly HashSet<char> Ambiguous = new("Il1O0|`'\";:,.".ToCharArray());

    public static string Generate(PasswordOptions o)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(o.Length, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(o.Length, 128);

        var pools = new List<string>(4);
        if (o.IncludeLower) pools.Add(Filter(Lower, o.ExcludeAmbiguous));
        if (o.IncludeUpper) pools.Add(Filter(Upper, o.ExcludeAmbiguous));
        if (o.IncludeDigits) pools.Add(Filter(Digits, o.ExcludeAmbiguous));
        if (o.IncludeSymbols) pools.Add(Filter(Symbols, o.ExcludeAmbiguous));

        if (pools.Count == 0)
            throw new InvalidOperationException("Enable at least one character set.");

        if (pools.Any(string.IsNullOrEmpty))
            throw new InvalidOperationException("Character set is empty after ambiguous exclusion.");

        string all = string.Concat(pools);
        var sb = new StringBuilder(o.Length);

        int mandatory = 0;
        if (o.EnsureEachCategory)
        {
            // Guarantee: at least one char from every enabled set.
            foreach (var pool in pools)
            {
                if (sb.Length >= o.Length) break;
                sb.Append(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
                mandatory++;
            }
        }

        for (int i = sb.Length; i < o.Length; i++)
            sb.Append(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // CSPRNG Fisher–Yates shuffle so mandatory chars are not predictable.
        char[] arr = sb.ToString().ToCharArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }

        // Best-effort: wipe intermediate builder.
        sb.Clear();
        return new string(arr);
    }

    public static int PoolSize(PasswordOptions o)
    {
        int n = 0;
        if (o.IncludeLower) n += Filter(Lower, o.ExcludeAmbiguous).Length;
        if (o.IncludeUpper) n += Filter(Upper, o.ExcludeAmbiguous).Length;
        if (o.IncludeDigits) n += Filter(Digits, o.ExcludeAmbiguous).Length;
        if (o.IncludeSymbols) n += Filter(Symbols, o.ExcludeAmbiguous).Length;
        return n;
    }

    /// <summary>Shannon entropy in bits: length * log2(poolSize).</summary>
    public static double EntropyBits(PasswordOptions o)
    {
        int pool = PoolSize(o);
        return pool <= 1 ? 0 : o.Length * Math.Log2(pool);
    }

    private static string Filter(string alphabet, bool excludeAmbiguous)
        => excludeAmbiguous ? new string(alphabet.Where(c => !Ambiguous.Contains(c)).ToArray()) : alphabet;
}
