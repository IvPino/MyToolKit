
namespace MyToolKit.Models;

public record PasswordAnalysis(
    int Length,
    bool HasLower,
    bool HasUpper,
    bool HasDigit,
    bool HasSymbol,
    double Entropy,
    bool IsCommon,
    StrengthLevel Strength)
{
    public static PasswordAnalysis Empty { get; } =
        new(0, false, false, false, false, 0, false, StrengthLevel.None);
}