// Contains all of the subsystems used by this plugin
using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;

namespace FFLogsPartyLookup
{
  public class Subsystems{
        private Configuration Configuration { get; init; }
        [PluginService][RequiredVersion("1.0")] public static CommandManager CommandManager { get; set; }
        [PluginService][RequiredVersion("1.0")] public static SigScanner SigScanner { get; set; }
        [PluginService][RequiredVersion("1.0")] public static DataManager DataManager { get; set; }
        [PluginService][RequiredVersion("1.0")] public static ClientState ClientState { get; set; }
        [PluginService][RequiredVersion("1.0")] public static ChatGui ChatGui { get; set; }
        [PluginService][RequiredVersion("1.0")] public static ChatHandlers ChatHandlers { get; set; }
        [PluginService][RequiredVersion("1.0")] public static GameGui GameGui { get; set; }
        [PluginService][RequiredVersion("1.0")] public static PartyList PartyList { get; set; }
  }
}
