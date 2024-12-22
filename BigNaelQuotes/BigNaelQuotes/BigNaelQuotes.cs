using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;

namespace BigNaelQuotes;

public class BigNaelQuotes : IDalamudPlugin
{
    public static string Name => "Big Nael Quotes";
    private const string CommandName = "/bigquotes";
    private static bool _drawConfiguration;
    private readonly Configuration _configuration;
    private readonly IClientState _clientState;
    private readonly IChatGui _chatGui;
    private readonly NaelQuotes _naelQuotes;

    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;

    public BigNaelQuotes(IDalamudPluginInterface dalamudPluginInterface, IChatGui chatGui, ICommandManager commandManager, IClientState clientState)
    {
        _chatGui = chatGui;
        _clientState = clientState;

        _configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
        _configuration.Initialize(dalamudPluginInterface);

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BigNaelQuotes.NaelQuotes.json");
        using var streamReader = new StreamReader(stream);
        var json = streamReader.ReadToEnd();
        _naelQuotes = JsonSerializer.Deserialize<NaelQuotes>(json);

        dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
        dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        dalamudPluginInterface.UiBuilder.OpenMainUi += OpenConfig;
            
        commandManager.AddHandler(CommandName, new CommandInfo(NaelCommand)
        {
            HelpMessage = "Open the configuration window",
            ShowInHelp = true
        });
            
        _chatGui.ChatMessage += OnChatMessage;
    }

    private void NaelCommand(string command, string args)
    {
        OpenConfig();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool handled)
    {
        if (!_configuration.Enabled) 
            return;

        if (type != XivChatType.NPCDialogueAnnouncements)
            return;

        foreach (var payload in message.Payloads)
        {
            if (payload is TextPayload { Text: not null } textPayload 
                && (sender.ToString().Contains("nael", StringComparison.OrdinalIgnoreCase) || sender.ToString().Contains("ネール", StringComparison.OrdinalIgnoreCase)) || sender.ToString().Contains("奈尔", StringComparison.OrdinalIgnoreCase)))
            {
                ShowTextGimmick(textPayload.Text);
            }
        }
    }
    
    private unsafe void ShowTextGimmick(string message)
    {
        RaptureAtkModule.Instance()->ShowTextGimmickHint(
            message,
            RaptureAtkModule.TextGimmickHintStyle.Warning,
            10 * _configuration.TextDisplayDurationSeconds);
    }

    private void DrawConfiguration()
    {
        if (!_drawConfiguration)
            return;
            
        ImGui.Begin($"{Name} Configuration", ref _drawConfiguration);
        
        var enabled = _configuration.Enabled;
        if (ImGui.Checkbox("Enable plugin", ref enabled))
        {
            _configuration.Enabled = enabled;
        }
        
        ImGui.PushItemWidth(150f * ImGuiHelpers.GlobalScale);
        var duration = _configuration.TextDisplayDurationSeconds;
        if (ImGui.InputInt("Quote display duration (seconds)", ref duration, 1))
        {
            if (duration < 1) duration = 1;
            if (duration > 60) duration = 60;
            _configuration.TextDisplayDurationSeconds = duration;
        }
        
        ImGui.Separator();

#if DEBUG
        if (ImGui.Button("Test quote in a random language"))
        {
            ShowTextGimmick(GetQuote(new Random().Next(0, 12), new Random().Next(0, 4)));
        }
#endif        
        if (ImGui.Button("Test quote in your language"))
        {
            ShowTextGimmick(GetQuote(new Random().Next(0, 12)));
        }

        if (ImGui.Button("Save"))
        {
            _configuration.Save();
        }
        
        ImGui.End();
    }

    /// <summary>
    /// uses the API quote ID to get the quote matching the client's language from the embedded NaelQuotes.json
    /// quotes taken from https://xivapi.com/NpcYell/[id] and https://cafemaker.wakingsands.com/NpcYell/[id]
    /// code taken from https://github.com/Eisenhuth/dalamud-nael/blob/master/nael/nael/NaelPlugin.cs 
    /// </summary>
    /// <param name="id">the quote ID</param>
    /// <returns>the quote based on the ID and the client language</returns>
    private string GetQuote(int id)
    {
        Quote quote = _naelQuotes.Quotes[id];
        string quoteText;
        switch (_clientState.ClientLanguage)
        {
            case ClientLanguage.Japanese:
                return quote.Text.JP.Replace("\n\n", "\n");
            case ClientLanguage.English:
                return quote.Text.EN.Replace("\n\n", "\n");
            case ClientLanguage.German:
                return quote.Text.DE.Replace("\n\n", "\n");
            case ClientLanguage.French:
                return quote.Text.FR.Replace("\n\n", "\n");
            default:
                return quote.Text.CN.Replace("\n\n", "\n");
        }
    }

    /// <summary>
    /// uses the API quote ID to get the quote matching the client's language from the embedded NaelQuotes.json
    /// quotes taken from https://xivapi.com/NpcYell/[id] and https://cafemaker.wakingsands.com/NpcYell/[id]
    /// code taken from https://github.com/Eisenhuth/dalamud-nael/blob/master/nael/nael/NaelPlugin.cs 
    /// </summary>
    /// <param name="id">the quote ID</param>
    /// <returns>the quote based on the ID and the client language</returns>
    private string GetQuote(int id, int language)
    {
        Quote quote = _naelQuotes.Quotes[id];
        switch (language)
        {
            case ((int)ClientLanguage.Japanese):
                return quote.Text.JP.Replace("\n\n", "\n");
            case (int)ClientLanguage.English:
                return quote.Text.EN.Replace("\n\n", "\n");
            case (int)ClientLanguage.German:
                return quote.Text.DE.Replace("\n\n", "\n");
            case (int)ClientLanguage.French:
                return quote.Text.FR.Replace("\n\n", "\n");
            case 4:
                return quote.Text.CN.Replace("\n\n", "\n");
            default:
                return quote.Text.CN.Replace("\n\n", "\n");
        }
    }

    private static void OpenConfig()
    {
        _drawConfiguration = true;
    }

    public void Dispose()
    {
        _chatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw -= DrawConfiguration;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

        CommandManager.RemoveHandler(CommandName);
    }
}