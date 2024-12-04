using Dalamud.Configuration;
using Dalamud.Plugin;

namespace BigNaelQuotes;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }
    public bool Enabled { get; set; } = true;
    public string CharacterName { get; set; } = "Nael deus Darnus";
    public int TextDisplayDurationSeconds { get; set; } = 4;

    private IDalamudPluginInterface _pluginInterface;

    public void Initialize(IDalamudPluginInterface pInterface)
    {
        _pluginInterface = pInterface;
    }

    public void Save()
    {
        _pluginInterface.SavePluginConfig(this);
    }
}