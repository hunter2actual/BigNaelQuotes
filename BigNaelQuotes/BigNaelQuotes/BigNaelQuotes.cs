using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FuzzySharp;
using ImGuiNET;
using nael;

namespace BigNaelQuotes;

public class BigNaelQuotes : IDalamudPlugin
{
    public static string Name => "Big Nael Quotes";
    private const string CommandName = "/nael";
    private static bool _drawConfiguration;
        
    private readonly Configuration _configuration;
    private readonly IChatGui _chatGui;
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] private static IClientState ClientState { get; set; } = null!;

    private string _configDynamo;
    private string _configChariot;
    private string _configBeam;
    private string _configDive;
    private string _configMeteorStream;
    private string _configSeparator;

    private readonly NaelQuotes _naelQuotes;
    private Dictionary<string, string> _naelQuotesDictionary;

    public BigNaelQuotes(IDalamudPluginInterface dalamudPluginInterface, IChatGui chatGui, ICommandManager commandManager)
    {
        _chatGui = chatGui;

        _configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
        _configuration.Initialize(dalamudPluginInterface);

        LoadConfiguration();
            
        dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
        dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        dalamudPluginInterface.UiBuilder.OpenMainUi += OpenConfig;
            
        commandManager.AddHandler(CommandName, new CommandInfo(NaelCommand)
        {
            HelpMessage = "toggle the plugin\n/nael test → print test quotes\n/nael cfg → open the configuration window",
            ShowInHelp = true
        });
            
        _chatGui.ChatMessage += OnChatMessage;
            
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BigNaelQuotes.NaelQuotes.json");
        using var streamReader = new StreamReader(stream);
        var json = streamReader.ReadToEnd();
        _naelQuotes = JsonSerializer.Deserialize<NaelQuotes>(json);
            
        LoadQuotesDictionary();
    }

    private void NaelCommand(string command, string args)
    {
        switch (args.ToLower())
        {
            case "cfg":
                OpenConfig();
                break;
            case "test":
                TestPlugin();
                break;
            default:
            {
                _configuration.Enabled = !_configuration.Enabled;
                _configuration.Save();

                var pluginStatus = _configuration.Enabled ? "enabled" : "disabled";
                _chatGui.Print($"{Name} {pluginStatus}");
                break;
            }
        }
    }

    private void TestPlugin()
    {
        foreach (var quote in _naelQuotes.Quotes) 
            _chatGui.Print(NaelMessage($"{GetQuote(quote.ID)}"));
    }

    private static XivChatEntry NaelMessage(string message)
    {
        var entry = new XivChatEntry
        {
            Name = "Nael deus Darnus",
            Type = XivChatType.NPCDialogueAnnouncements,
            Message = message
        };

        return entry;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool handled)
    {
        if (!_configuration.Enabled) 
            return;

        if (type != XivChatType.NPCDialogueAnnouncements)
            return;

        foreach (var payload in message.Payloads)
            if (payload is TextPayload { Text: not null } textPayload)
            {
                textPayload.Text = NaelIt(textPayload.Text);
            }
    }
        
    /// <summary>
    /// checks the chat message for any Nael quote and replaces it with the mechanics
    /// </summary>
    /// <param name="input">chat message</param>
    /// <returns>the names of the mechanics or the chat message if no quotes are found</returns>
    private string NaelIt(string input)
    {
        var match = Process.ExtractOne(input, _naelQuotesDictionary.Keys, (s) => s);
            
        return match is { Score: >= 85 } ? _naelQuotesDictionary[match.Value] : input;
    }

    /// <summary>
    /// uses the API quote ID to get the quote matching the client's language from the embedded NaelQuotes.json
    /// quotes taken from https://xivapi.com/NpcYell/[id] and https://cafemaker.wakingsands.com/NpcYell/[id]
    /// </summary>
    /// <param name="id">the quote ID</param>
    /// <returns>the quote based on the ID and the client language</returns>
    private string GetQuote(int id)
    {
        var quote = _naelQuotes.Quotes.Find(q => q.ID == id);
        var quoteText =  ClientState.ClientLanguage switch
        {
            ClientLanguage.English => quote.Text.Text_en,
            ClientLanguage.French => quote.Text.Text_fr,
            ClientLanguage.German => quote.Text.Text_de,
            ClientLanguage.Japanese => quote.Text.Text_ja,
            _ => quote.Text.Text_chs
        };
        quoteText = quoteText.Replace("\n\n", "\n");
            
        return quoteText;
    }

    /// <summary>
    /// loads all quotes from the embedded .json into a dictionary
    /// </summary>
    private void LoadQuotesDictionary()
    {
        _naelQuotesDictionary = new Dictionary<string, string>
        {
            {GetQuote(6492), $"{_configDynamo} {_configSeparator} {_configChariot}"}, //Phase 2 - Nael
            {GetQuote(6493), $"{_configDynamo} {_configSeparator} {_configBeam}"},
            {GetQuote(6494), $"{_configBeam} {_configSeparator} {_configChariot}"},
            {GetQuote(6495), $"{_configBeam} {_configSeparator} {_configDynamo}"},
            {GetQuote(6496), $"{_configDive} {_configSeparator} {_configChariot}"},
            {GetQuote(6497), $"{_configDive} {_configSeparator} {_configDynamo}"},
            {GetQuote(6500), $"{_configMeteorStream} {_configSeparator} {_configDive}"},
            {GetQuote(6501), $"{_configDive} {_configSeparator} {_configBeam}"},
            {GetQuote(6502), $"{_configDive} {_configSeparator} {_configDynamo} {_configSeparator} {_configMeteorStream}"}, //Phase 3 - Bahamut Prime
            {GetQuote(6503), $"{_configDynamo} {_configSeparator} {_configDive} {_configSeparator} {_configMeteorStream}"},
            {GetQuote(6504), $"{_configChariot} {_configSeparator} {_configBeam} {_configSeparator} {_configDive}"}, //Phase 4 - Adds
            {GetQuote(6505), $"{_configChariot} {_configSeparator} {_configDive} {_configSeparator} {_configBeam}"},
            {GetQuote(6506), $"{_configDynamo} {_configSeparator} {_configDive} {_configSeparator} {_configBeam}"},
            {GetQuote(6507), $"{_configDynamo} {_configSeparator} {_configChariot} {_configSeparator} {_configDive}"}
        };
    }

    private void DrawConfiguration()
    {
        if (!_drawConfiguration)
            return;
            
        ImGui.Begin($"{Name} Configuration", ref _drawConfiguration);
            
        ImGui.InputText("Dynamo", ref _configDynamo, 32);
        ImGui.InputText("Chariot", ref _configChariot, 32);
        ImGui.InputText("Beam", ref _configBeam, 32);
        ImGui.InputText("Dive", ref _configDive, 32);
        ImGui.InputText("Meteor Stream", ref _configMeteorStream, 32);
        ImGui.InputText("Separator", ref _configSeparator, 8);
            
        ImGui.Separator();
            
        ImGui.Text($"Nael deus Darnus: {_configBeam} {_configSeparator} {_configChariot}");
        ImGui.Text($"Nael deus Darnus: {_configDynamo} {_configSeparator} {_configDive} {_configSeparator} {_configMeteorStream}");
            
        ImGui.Separator();        
            
        if (ImGui.Button("Save and Close"))
        {
            SaveConfiguration();
            LoadQuotesDictionary();
            _drawConfiguration = false;
        }
            
        ImGui.SameLine();
            
        if (ImGui.Button("Test all quotes"))
        {
            TestPlugin();
        }
            
        ImGui.End();
    }

    private static void OpenConfig()
    {
        _drawConfiguration = true;
    }

    private void LoadConfiguration()
    {
        _configDynamo = _configuration.Dynamo;
        _configChariot = _configuration.Chariot;
        _configBeam = _configuration.Beam;
        _configDive = _configuration.Dive;
        _configMeteorStream = _configuration.MeteorStream;
        _configSeparator = _configuration.Separator;
    }

    private void SaveConfiguration()
    {
        _configuration.Dynamo = _configDynamo;
        _configuration.Chariot = _configChariot;
        _configuration.Beam = _configBeam;
        _configuration.Dive = _configDive;
        _configuration.MeteorStream = _configMeteorStream;
        _configuration.Separator = _configSeparator;
            
        PluginInterface.SavePluginConfig(_configuration);
    }

    public void Dispose()
    {
        _chatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw -= DrawConfiguration;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

        CommandManager.RemoveHandler(CommandName);
    }
}