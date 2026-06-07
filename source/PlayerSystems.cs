using System.Text.Json;

namespace PythonCoderGame;

internal sealed class UserRegistry
{
    public List<UserProfile> Users { get; set; } = [];
}

internal sealed class UserProfile
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Callsign { get; set; } = "";
    public int Rank { get; set; } = 1;
    public int Xp { get; set; }
    public int ScrapTokens { get; set; }
    public int TotalScore { get; set; }
    public int MissionsCompleted { get; set; }
    public int BestWpm { get; set; }
    public double BestAccuracy { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> Upgrades { get; set; } = new()
    {
        ["cpu"] = 1,
        ["gpu"] = 1,
        ["nvme"] = 1,
        ["ram"] = 1
    };

    public string RankName => Rank switch
    {
        1 => "BEGINNER",
        2 => "APPRENTICE",
        3 => "PYTHON BUILDER",
        4 => "CODE OPERATOR",
        _ => "PYTHON SPECIALIST"
    };
}

internal static class ProfileStore
{
    private static readonly string StoreDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PythonCoderGame");

    private static readonly string StorePath = Path.Combine(StoreDir, "users.json");

    public static UserRegistry LoadRegistry()
    {
        try
        {
            if (!File.Exists(StorePath))
            {
                return new UserRegistry();
            }

            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<UserRegistry>(json) ?? new UserRegistry();
        }
        catch
        {
            return new UserRegistry();
        }
    }

    public static void SaveRegistry(UserRegistry registry)
    {
        Directory.CreateDirectory(StoreDir);
        var json = JsonSerializer.Serialize(registry, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(StorePath, json);
    }

    public static UserProfile CreateUser(string firstName, string lastName, string callsign)
    {
        var registry = LoadRegistry();
        if (registry.Users.Any(u => u.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("That callsign is already registered.");
        }

        var user = new UserProfile
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Callsign = callsign.Trim()
        };

        registry.Users.Add(user);
        SaveRegistry(registry);
        return user;
    }

    public static UserProfile? LoadUser(string callsign)
    {
        return LoadRegistry().Users.FirstOrDefault(u => u.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase));
    }

    public static void SaveUser(UserProfile user)
    {
        var registry = LoadRegistry();
        var index = registry.Users.FindIndex(u => u.Callsign.Equals(user.Callsign, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            registry.Users[index] = user;
        }
        else
        {
            registry.Users.Add(user);
        }

        SaveRegistry(registry);
    }
}

internal sealed class ScoreEngine
{
    private readonly UpgradeEffects _effects;
    private DateTime _startedUtc = DateTime.UtcNow;

    public ScoreEngine(UpgradeEffects effects)
    {
        _effects = effects;
    }

    public int Score { get; private set; }
    public int CorrectChars { get; private set; }
    public int TotalChars { get; private set; }
    public int ErrorChars { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public double Multiplier { get; private set; } = 1.0;
    public int PerfectLines { get; private set; }
    public int LastPenalty { get; private set; }

    public void Reset()
    {
        _startedUtc = DateTime.UtcNow;
        Score = 0;
        CorrectChars = 0;
        TotalChars = 0;
        ErrorChars = 0;
        Combo = 0;
        MaxCombo = 0;
        Multiplier = 1.0;
        PerfectLines = 0;
        LastPenalty = 0;
    }

    public bool SubmitLine(string typed, string target)
    {
        LastPenalty = 0;
        var perfect = typed == target;
        var max = Math.Max(typed.Length, target.Length);
        for (var i = 0; i < max; i++)
        {
            var ok = i < typed.Length && i < target.Length && typed[i] == target[i];
            if (ok)
            {
                CorrectChars++;
                TotalChars++;
                Combo++;
                MaxCombo = Math.Max(MaxCombo, Combo);
                UpdateMultiplier();
                Score += (int)Math.Round(10 * Multiplier);
            }
            else
            {
                TotalChars++;
                ErrorChars++;
                Combo = 0;
                Multiplier = 1.0;
                LastPenalty += 25;
                Score -= 25;
            }
        }

        if (perfect)
        {
            PerfectLines++;
            Score += (int)Math.Round(200 * Multiplier);
        }

        return perfect;
    }

    public void ApplyLiveTypoPenalty(int amount)
    {
        LastPenalty = Math.Max(0, amount);
        Score -= LastPenalty;
        Combo = 0;
        Multiplier = 1.0;
    }

    public int Wpm
    {
        get
        {
            var elapsed = Math.Max(0.05, (DateTime.UtcNow - _startedUtc).TotalMinutes);
            return Math.Max(0, (int)Math.Round((CorrectChars / 5.0) / elapsed));
        }
    }

    public double Accuracy => TotalChars == 0 ? 100 : Math.Round((CorrectChars / (double)TotalChars) * 100, 2);

    public int TokensEarned => Math.Max(1, (int)Math.Round((PerfectLines + Math.Max(0, Score / 2500.0)) * _effects.TokenMultiplier));

    public int XpEarned => Math.Max(0, Score / 5 + PerfectLines * 75);

    private void UpdateMultiplier()
    {
        var thresholds = new[] { (0, 1.0), (10, 1.5), (25, 2.0), (50, 3.0), (100, 5.0) };
        foreach (var (threshold, multiplier) in thresholds)
        {
            if (Combo >= Math.Floor(threshold * _effects.ComboThresholdMod))
            {
                Multiplier = multiplier;
            }
        }
    }
}

internal sealed record UpgradeTier(int Tier, string Name, int Cost, string Effect);

internal sealed record UpgradeCategory(string Id, string Name, string Description, IReadOnlyList<UpgradeTier> Tiers);

internal sealed record UpgradeEffects(
    double SpeedMod,
    double ComboThresholdMod,
    double TimeMod,
    int ErrorThreshold,
    double TokenMultiplier);

internal static class UpgradeSystem
{
    public static IReadOnlyList<UpgradeCategory> Categories { get; } =
    [
        new("cpu", "CPU", "Processing speed reduces scroll pressure.", [
            new(1, "Intel Core i5-13600K", 0, "Base processing"),
            new(2, "Intel Core i7-14700K", 5, "-5% scroll pressure"),
            new(3, "Intel Core i9-14900KS", 15, "-10% scroll pressure"),
            new(4, "AMD Threadripper 7980X", 40, "-15% scroll pressure"),
            new(5, "AMD EPYC 9654", 100, "-20% scroll pressure")
        ]),
        new("gpu", "GPU", "Render pipeline unlocks combo tiers faster.", [
            new(1, "NVIDIA RTX 3060 12GB", 0, "Base combo tiers"),
            new(2, "NVIDIA RTX 3080 10GB", 5, "Combo tiers 20% faster"),
            new(3, "NVIDIA RTX 4090 24GB", 15, "Combo tiers 35% faster"),
            new(4, "NVIDIA A100 80GB", 40, "Combo tiers 50% faster"),
            new(5, "NVIDIA H100 80GB HBM3", 100, "Combo tiers 65% faster")
        ]),
        new("nvme", "NVMe", "Storage throughput gives more learning time.", [
            new(1, "500GB SATA SSD", 0, "Base timer"),
            new(2, "1TB Samsung 970 EVO", 5, "+10% line time"),
            new(3, "2TB Samsung 980 PRO", 15, "+20% line time"),
            new(4, "4TB Samsung 990 PRO", 40, "+30% line time"),
            new(5, "8TB Enterprise NVMe", 100, "+40% line time")
        ]),
        new("ram", "RAM", "Error buffer softens beginner mistakes.", [
            new(1, "16GB DDR4-3200", 0, "Base tolerance"),
            new(2, "32GB DDR5-5600", 5, "+1 error buffer"),
            new(3, "64GB DDR5-6400", 15, "+2 error buffer"),
            new(4, "128GB DDR5-7200", 40, "+3 error buffer"),
            new(5, "256GB DDR5-8000", 100, "+4 error buffer")
        ])
    ];

    public static UpgradeEffects GetEffects(UserProfile? user)
    {
        var cpu = GetTier(user, "cpu");
        var gpu = GetTier(user, "gpu");
        var nvme = GetTier(user, "nvme");
        var ram = GetTier(user, "ram");

        return new UpgradeEffects(
            +(1.0 - (cpu - 1) * 0.05),
            +(1.0 - (gpu - 1) * 0.15),
            +(1.0 + (nvme - 1) * 0.10),
            ram - 1,
            +(1.0 + (gpu - 1) * 0.10));
    }

    public static int GetTier(UserProfile? user, string id)
    {
        if (user?.Upgrades.TryGetValue(id, out var tier) == true)
        {
            return Math.Clamp(tier, 1, 5);
        }

        return 1;
    }

    public static UpgradeTier? Current(UserProfile user, string id)
    {
        var category = Categories.First(c => c.Id == id);
        return category.Tiers.First(t => t.Tier == GetTier(user, id));
    }

    public static UpgradeTier? Next(UserProfile user, string id)
    {
        var category = Categories.First(c => c.Id == id);
        var next = GetTier(user, id) + 1;
        return category.Tiers.FirstOrDefault(t => t.Tier == next);
    }

    public static string Purchase(UserProfile user, string id)
    {
        var next = Next(user, id);
        if (next is null)
        {
            return "Already maxed.";
        }

        if (user.ScrapTokens < next.Cost)
        {
            return $"Need {next.Cost} scrap tokens.";
        }

        user.ScrapTokens -= next.Cost;
        user.Upgrades[id] = next.Tier;
        ProfileStore.SaveUser(user);
        return $"Purchased {next.Name}.";
    }
}
