using System;
using System.Text;
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
    private readonly IChatGui _chatGui;

    private static readonly string[] NaelQuotesEn =
    [
        "O hallowed moon,\nshine you the iron path!",
        "O hallowed moon,\ntake fire and scorch my foes!",
        "Blazing path,\nlead me to iron rule!",
        "Take fire,\nO hallowed moon!",
        "From on high I descend,\nthe iron path to walk!",
        "From on high I descend,\nthe hallowed moon to call!",
        "Fleeting light!\nAmid a rain of stars,\nexalt you the red moon!",
        "Fleeting light!\n'Neath the red moon,\nscorch you the earth!",
        "From on high I descend,\nthe moon and stars to bring!",
        "From hallowed moon I descend,\na rain of stars to bring!",
        "Unbending iron,\ntake fire and descend!",
        "Unbending iron,\ndescend with fiery edge!",
        "From hallowed moon I descend,\nupon burning earth to tread!",
        "From hallowed moon I bare iron,\nin my descent to wield!"
    ];
    
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;

    public BigNaelQuotes(IDalamudPluginInterface dalamudPluginInterface, IChatGui chatGui, ICommandManager commandManager)
    {
        _chatGui = chatGui;

        _configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
        _configuration.Initialize(dalamudPluginInterface);
        
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
                && sender.ToString().Contains("nael", StringComparison.OrdinalIgnoreCase))
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
        
        if (ImGui.Button("Test quote"))
        {
            var randomQuote = NaelQuotesEn[Random.Shared.Next(NaelQuotesEn.Length)];
            ShowTextGimmick(randomQuote);
        }

        if (ImGui.Button("Save"))
        {
            _configuration.Save();
        }
        
        ImGui.End();
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