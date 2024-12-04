using System;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Components;
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
                && sender.ToString().Equals(_configuration.CharacterName, StringComparison.Ordinal))
            {
                ShowTextGimmick(textPayload.Text, _configuration.TextDisplayDurationSeconds);
            }
        }
    }
    
    private static unsafe void ShowTextGimmick(string message, int durationInSeconds)
    {
        RaptureAtkModule.Instance()->ShowTextGimmickHint(
            Encoding.UTF8.GetBytes(message),
            RaptureAtkModule.TextGimmickHintStyle.Warning,
            10 * durationInSeconds);
    }

    private void DrawConfiguration()
    {
        if (!_drawConfiguration)
            return;
            
        ImGui.Begin($"{Name} Configuration", ref _drawConfiguration);
        ImGui.PushItemWidth(150f * ImGuiHelpers.GlobalScale);

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
        {
            _configuration.CharacterName = "Nael deus Darnus";
        }
        ImGui.SameLine();
        var characterName = _configuration.CharacterName;
        if (ImGui.InputText("NPC Name to match on", ref characterName, 32))
        {
            _configuration.CharacterName = characterName.Trim();
        }

        var duration = _configuration.TextDisplayDurationSeconds;
        if (ImGui.InputInt("Quote display duration (seconds)", ref duration, 1))
        {
            if (duration < 1) duration = 1;
            if (duration > 60) duration = 60;
            _configuration.TextDisplayDurationSeconds = duration;
        }
        
        ImGui.Separator();
        
        if (ImGui.Button("Test message"))
        {
            ShowTextGimmick("From hallowed moon I bare iron,\nin my descent to wield!", _configuration.TextDisplayDurationSeconds);
        }
        
        if (ImGui.Button("Save")) _configuration.Save();
        
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