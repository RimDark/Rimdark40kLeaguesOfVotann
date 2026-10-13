using Core40k;
using HarmonyLib;
using Verse;

namespace Votann40k;

public class Votann40kMod : CoreMod
{
    public static string CurrentVersion;

    public static Harmony harmony;

    public static Votann40kModSettings VotannSettings;

    private Votann40kModSettings settings;
    public override ModSettings Settings => settings ??= GetSettings<Votann40kModSettings>();

    public Votann40kMod(ModContentPack content) : base(content)
    {
        harmony = new Harmony("Votann40k.Mod");
        CurrentVersion = content.ModMetaData.ModVersion;
        settings = GetSettings<Votann40kModSettings>();
        VotannSettings = settings;

        harmony.PatchAll();
    }

    private readonly ModSettingTab_VotannMain votannMainSettings = new();

    public override void InitializeTabs()
    {
        var mainTab = new TabRecord("BEWH.Votann.ModSettings.TabMain".Translate(), delegate
        {
            currentSettingTab = votannMainSettings;
        }, () => currentSettingTab == votannMainSettings);
        tabs.Add(mainTab);

        currentSettingTab = votannMainSettings;
        base.InitializeTabs();
    }

    public override string SettingsCategory()
    {
        return "BEWH.Votann.ModSettings.ModName".Translate(CurrentVersion);
    }
}
