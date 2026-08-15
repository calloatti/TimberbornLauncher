using System;
using System.Linq;

namespace TimberbornLauncher.Versioning;

/// <summary>
/// Replica of Timberborn's Version type, used to pick the matching version-* mod folder.
/// Implements the same numeric comparison as the game's IsEqualOrHigherThan.
/// </summary>
public sealed class GameVersion : IComparable<GameVersion>
{
    private readonly int[] _subNumbers;

    public string Numeric { get; }

    public bool IsDevelopmentVersion => Numeric == "0";

    private GameVersion(string numeric, int[] subNumbers)
    {
        Numeric = numeric;
        _subNumbers = subNumbers;
    }

    public static GameVersion? TryCreate(string version)
    {
        string? numeric = TryExtractVersionNumber(version);
        if (numeric == null)
        {
            return null;
        }
        int[] subNumbers = new int[0];
        foreach (string part in numeric.Split('.'))
        {
            if (!int.TryParse(part, out int value))
            {
                return null;
            }
            subNumbers = subNumbers.Append(value).ToArray();
        }
        return new GameVersion(numeric, subNumbers);
    }

    public bool IsEqualOrHigherThan(GameVersion other)
    {
        for (int i = 0; i < _subNumbers.Length; i++)
        {
            if (i >= other._subNumbers.Length)
            {
                return true;
            }
            if (_subNumbers[i] > other._subNumbers[i])
            {
                return true;
            }
            if (_subNumbers[i] < other._subNumbers[i])
            {
                return false;
            }
        }
        for (int j = _subNumbers.Length; j < other._subNumbers.Length; j++)
        {
            if (other._subNumbers[j] > 0)
            {
                return false;
            }
        }
        return true;
    }

    public int CompareTo(GameVersion? other)
    {
        if (other == null)
        {
            return 1;
        }
        if (IsEqualOrHigherThan(other))
        {
            return other.IsEqualOrHigherThan(this) ? 0 : 1;
        }
        return -1;
    }

    private static string? TryExtractVersionNumber(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0.0";
        }
        if (version[0] == 'v')
        {
            return "0.0.0.0";
        }
        return version.Split('-')[0];
    }
}
