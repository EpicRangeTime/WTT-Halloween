using JetBrains.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace WTT_Halloween;

[UsedImplicitly]
public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.wtt.halloween";
    public string Name { get; init; } = "WTT Halloween";
    public string Author { get; init; } = "EpicRangeTime";
    public List<string>? Contributors { get; init; } = [];
    public Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version!.ToString(3));
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~3.0.6") }
    };
    public string? Url { get; init; } = "https://github.com/EpicRangeTime/WTT-Halloween";
    public string License { get; init; } = "CC-BY-NC-ND 4.0";
}