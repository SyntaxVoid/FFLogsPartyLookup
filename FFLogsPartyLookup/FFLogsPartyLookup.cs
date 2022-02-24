using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;


namespace FFLogsPartyLookup
{
  public unsafe class FFLogsPartyLookup : IDalamudPlugin
  {
    public string Name => "FFLogs Party Lookup";
    private const string CommandName = "/pfflogs";
    public static DalamudPluginInterface PluginInterface { get; private set; }

    public FFLogsPartyLookup(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] SigScanner sigScanner,
        [RequiredVersion("1.0")] DataManager dataManager,
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] ChatGui chatGui,
        [RequiredVersion("1.0")] ChatHandlers chatHandlers,
        [RequiredVersion("1.0")] GameGui gameGui,
        [RequiredVersion("1.0")] PartyList partyList
        )
    {
      PluginInterface = pluginInterface;
      Subsystems.CommandManager = commandManager;
      Subsystems.CommandManager = commandManager;
      Subsystems.SigScanner = sigScanner;
      Subsystems.DataManager = dataManager;
      Subsystems.ClientState = clientState;
      Subsystems.ChatGui = chatGui;
      Subsystems.ChatHandlers = chatHandlers;
      Subsystems.GameGui = gameGui;
      Subsystems.PartyList = partyList;

      Subsystems.CommandManager.AddHandler(CommandName, new CommandInfo(pfflogsCommand)
      {
        HelpMessage = "Shows links to your party members' FFLogs in the /echo channel"
      });
      PartyHandler.Init();
      // PluginInterface.UiBuilder.Draw += DrawUI;
      // PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
      // this.UI.Dispose();
      Subsystems.CommandManager.RemoveHandler(CommandName);
    }

    private void pfflogsCommand(string command, string args)
    {
      // This is run when the user enters /pfflogs in chat. Collects a list
      // of playerInfo objects for everyone in the party and then displays
      // their names and links to their fflogs page

      string templateMessage = "{0}: {1}"; // Name: Url
      string tempName;
      string tempUrl;
      List<playerInfo> pInfo = PartyHandler.getInfoFromParty();
      if (pInfo == null)
      {
        echo("Unexpected party type. Maybe a Trust?");
        return;
      }
      int pCount = pInfo.Count;
      for (int i = 0; i < pCount; i++)
      {
        // "{Name} ({Job}): {Url}";
        tempName = pInfo[i].playerName;
        tempUrl = PartyHandler.urlFromPlayerInfo(pInfo[i]);
        
        PlayerCharacter you = Subsystems.ClientState.LocalPlayer;
        string yourName = you.Name.ToString();
        if (tempName.Equals(yourName)) // it u!
        {
          echo(String.Format(templateMessage, "You", tempUrl));
        }
        else
        {
          echo(String.Format(templateMessage, tempName, tempUrl));
        }
      }
    }

    public void echo(string s)
    {
      // Prints a message to the /echo channel
      XivChatEntry msg = new XivChatEntry()
      {
        Message = s,
        Type = XivChatType.Echo
      };
      Subsystems.ChatGui.PrintChat(msg);
      return;
    }
    
    
    // private void DrawUI()
    // {
    //     this.UI.Draw();
    // }
    // private void DrawConfigUI()
    // {
    //     this.UI.SettingsVisible = true;
    // }
  }
}
