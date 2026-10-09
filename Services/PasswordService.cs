using System.Security.Cryptography;
using MyToolKit.Models;

namespace MyToolKit.Services;

public class PasswordService
{
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.<>?/";

    private readonly HttpClient _http;
    private HashSet<string> _commonPasswords = new(StringComparer.OrdinalIgnoreCase);

    public PasswordService(HttpClient http)
    {
        _http = http;
    }

    public bool IsListLoaded => _commonPasswords.Count > 0;

    // ===== Lista de contraseñas filtradas =====

    public async Task<bool> LoadCommonPasswordsAsync()
    {
        if (IsListLoaded)
            return true;

        try
        {
            var text = await _http.GetStringAsync("data/common_passwords.txt");
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _commonPasswords = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // ===== Análisis =====

    public PasswordAnalysis Analyze(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordAnalysis.Empty;

        bool hasLower = false, hasUpper = false, hasDigit = false, hasSymbol = false;

        foreach (char c in password)
        {
            if (char.IsLower(c)) hasLower = true;
            else if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSymbol = true;
        }

        int poolSize = 0;
        if (hasLower) poolSize += 26;
        if (hasUpper) poolSize += 26;
        if (hasDigit) poolSize += 10;
        if (hasSymbol) poolSize += 33;

        double entropy = password.Length * Math.Log2(poolSize);
        bool isCommon = _commonPasswords.Contains(password);
        StrengthLevel strength = isCommon ? StrengthLevel.VeryWeak : GetStrength(entropy);

        return new PasswordAnalysis(
            password.Length, hasLower, hasUpper, hasDigit, hasSymbol,
            entropy, isCommon, strength);
    }

    private static StrengthLevel GetStrength(double entropy) => entropy switch
    {
        < 28 => StrengthLevel.VeryWeak,
        < 36 => StrengthLevel.Weak,
        < 60 => StrengthLevel.Fair,
        < 80 => StrengthLevel.Strong,
        _ => StrengthLevel.VeryStrong
    };

    // ===== Generación =====

    public string Generate(int length, bool useLower, bool useUpper, bool useDigits, bool useSymbols)
    {
        var selectedSets = new List<string>();
        if (useLower) selectedSets.Add(LowerChars);
        if (useUpper) selectedSets.Add(UpperChars);
        if (useDigits) selectedSets.Add(DigitChars);
        if (useSymbols) selectedSets.Add(SymbolChars);

        if (selectedSets.Count == 0)
            return string.Empty;

        if (length < selectedSets.Count)
            throw new ArgumentOutOfRangeException(nameof(length),
                "Length must be at least the number of selected character types.");

        string pool = string.Concat(selectedSets);
        char[] result = new char[length];

        for (int i = 0; i < selectedSets.Count; i++)
        {
            string set = selectedSets[i];
            result[i] = set[RandomNumberGenerator.GetInt32(set.Length)];
        }

        for (int i = selectedSets.Count; i < length; i++)
        {
            result[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }

        RandomNumberGenerator.Shuffle(result.AsSpan());
        return new string(result);
    }
}