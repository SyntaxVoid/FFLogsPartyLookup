using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI;


namespace FFLogsPartyLookup
{
  public struct playerInfo
  {
    // Struct for holding the playerInfo relevent to our purposes. These values
    // are fetched using the various methods throughout the PartyHandler class
    public playerInfo(string name, string world, string region)
    {
      playerName   = name;
      playerWorld  = world;
      playerRegion = region;
    }
    public string playerName   { get; }
    public string playerWorld  { get; }
    public string playerRegion { get; }
    public override string ToString() => $"{playerName} [{playerWorld}, {playerRegion}]";
  }


  public unsafe class PartyHandler
  {
    // Somewhat abstractifies the interface needed to get a list of party members
    // This is not as straightforward as it seems since the game's memory treats
    // normal parties, solo parties, trust parties, light parties, full parties,
    // cross world parties, and any other parties you can think of, a bit
    // differently and special considerations need to be made for a couple of the
    // cases. To get a list of members from whatever party you're in, just call
    // PartyHandler.getInfoFromParty() 
    // which will delegate the fetching of info to one of several internal
    // methods depending on the party type (which you don't need to worry about)
    delegate IntPtr InfoProxyCrossRealm_GetPtr();
    static InfoProxyCrossRealm_GetPtr InfoProxyCrossRealm_GetPtrDelegate;
    public delegate byte GetCrossRealmPartySize();
    public static GetCrossRealmPartySize getCrossRealmPartySize;
    
    public static void Init()
    {
      // This needs to be started when the plugin is loaded to identify the
      // location in memory where the cross-world party exists. This will
      // probably break if SE ever changes the offsets or the layout
      // of the CrossRealmGroup class.
      // src: gist.github.com/Eternita-S/c21192996d181c41740c6322f2760e16
      IntPtr ipcr_ptr = Subsystems.SigScanner.ScanText("48 8B 05 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC 40 53 41 57");
      InfoProxyCrossRealm_GetPtrDelegate = Marshal.GetDelegateForFunctionPointer<InfoProxyCrossRealm_GetPtr>(ipcr_ptr);
      PluginLog.Information("InfoProxyCrossRealm_GetPtr: 0x" + ipcr_ptr.ToString("X"));
      IntPtr gcrps_ptr = Subsystems.SigScanner.ScanText("48 83 EC 28 E8 ?? ?? ?? ?? 84 C0 74 3C");
      getCrossRealmPartySize = Marshal.GetDelegateForFunctionPointer<GetCrossRealmPartySize>(gcrps_ptr);
      PluginLog.Information("GetCrossRealmPartySize: 0x" + gcrps_ptr.ToString("X"));
    }

    private static playerInfo GetCrossRealmPlayer(int index)
    {
      // Utilizes the results of the SigScanner from Init() to locate 
      // information about a specific crossworld player and build a 
      // playerInfo object based on the provided index. If there's any error, 
      // returns a default playerInfo object instead
      try
      {
        IntPtr playerPtr = InfoProxyCrossRealm_GetPtrDelegate() + 0x3c2 + 0x50 * index;
        string playerName = Marshal.PtrToStringUTF8(playerPtr + 0x8);
        string world = worldNameFromByte(*(byte*)playerPtr);
        string region = regionFromWorld(world);
        return new playerInfo(playerName, world, region);
      }
      catch
      {
        return new playerInfo("NotFound", "NotFound", "NotFound");
      }
    }

    private static string classJobFromByte(byte classJobByte)
    {
      // Given a classJob byte, returns a string correlating to the actual
      // classJob. This isn't really used since in regular parties, the job
      // is not known to the client unless you are in the same zone as the 
      // party member. In that case, the classJob ends up resolving to the
      // default "adventurer". The actual information is *somewhere* in the
      // memory since you can see the job in the party list... but I don't 
      // want to deal with that :3 + I don't really care that much.
      #pragma warning disable 8632
      ExcelSheet<ClassJob>? ClassJobSheet = Subsystems.DataManager.GetExcelSheet<ClassJob>();
      ClassJob[]? ClassJobs = ClassJobSheet?.ToArray();
      if (ClassJobs != null) // We found the ClassJobs!
      {
        ClassJob? ClassJob = Array.Find(ClassJobs, x => x.RowId == classJobByte);
        if (ClassJob != null)
        {
          return ClassJob.Name.ToString();
        }
        else
        {
          return $"UnknownClassJobForByteID={classJobByte.ToString()}";
        }
      }
      else
      {
        return "UnableToFindWorld";
      }
    #pragma warning restore 8632
    }

    private static string worldNameFromByte(byte worldByte)
    {
      // Given a byte for a specific world (SE decides how these are mapped), 
      // this method will search the Worlds excel sheet (provided by Dalamud)
      // to resolve to a string of the actual world name. This is basically the
      // same method as classJobFromByte but uses World instead of classJob and
      // I'm sure some kind of template could combine these two, but I'm not
      // familiar enough with templates for a robust solution.
      #pragma warning disable 8632
      ExcelSheet<World>? worldSheet = Subsystems.DataManager.GetExcelSheet<World>();
      World[]? worlds = worldSheet?.ToArray();
      if (worlds != null) // We found the worlds!
      {
        World? world = Array.Find(worlds, x => x.RowId == worldByte);
        if (world != null)
        {
          return world.Name.ToString();
        }
        else
        {
          return $"UnknownWorldForByteID={worldByte.ToString()}";
        }
      }
      else
      {
        return "UnableToFindWorld";
      }
    #pragma warning restore 8632
    }

    public static string regionFromWorld(string world)
    {
      // Returns a region code for the specified world/server. This method
      // exists because FFLogs includes the region code within the url for
      // characters. This will have to be manually updated if any changes
      // are made to the existing worlds.
    
      switch (world)
      {
        // Primal DC
        case "Behemoth": case "Excalibur": case "Exodus": case "Famfrit":
        case "Hyperion": case "Lamia": case "Leviathan": case "Ultros":

        // Aether DC
        case "Adamantoise": case "Cactuar": case "Faerie": case "Gilgamesh":
        case "Jenova": case "Midgardsormr": case "Sargatanas": case "Siren":
        
        // Crystal DC
        case "Balmung": case "Brynhildr": case "Coeurl": case "Diabolos":
        case "Goblin": case "Malboro": case "Mateus": case "Zalera":
        
        return "na";


        // Elemental DC
        case "Aegis": case "Atomos": case "Carbuncle": case "Garuda":
        case "Gungnir": case "Kujata": case "Ramuh": case "Tonberry":
        case "Typhon": case "Unicorn":

        // Gaia DC
        case "Alexander": case "Bahamut": case "Durandal": case "Fenrir":
        case "Ifrit": case "Ridill": case "Tiamat": case "Ultima":
        case "Valefor": case "Yojimbo": case "Zeromus":

        // Mana DC
        case "Anima": case "Asura": case "Belias": case "Chocobo": 
        case "Hades": case "Ixon": case "Mandragora": case "Masamune":
        case "Pandaemonium": case "Shinryu": case "Titan":

        return "jp";


        // Chaos DC
        case "Cerberus": case "Louisoix": case "Moogle": case "Omega":
        case "Ragnarok": case "Spriggan":

        // Light DC
        case "Lich": case "Odin": case "Phoenix": case "Shiva":
        case "Twintania": case "Zodiark":
        
        return "eu";


        // Materia DC
        case "Bismarck": case "Ravana": case "Sephirot": case "Sophia":
        case "Zurvan":

        return "oc";


        default:
        return "RegionNotFound";
      }
    }

    public static string urlFromPlayerInfo(playerInfo player)
    {
      // Returns the FFLogs url for a given playerInfo object
      return $"https://www.fflogs.com/character/{player.playerRegion}/{player.playerWorld}/{Uri.EscapeDataString(player.playerName)}";
    }
  
    private static int getPartyType()
    {
      // Gets the type of party by inspecting the _PartyList object
      // Returns:
      //     0: Player is in a solo party
      //     1: Player is in a normal party (non-cross world)
      //     2: Player is in a cross-world party
      //     3: Player is in an alliance group
      //    -1: Player is in none of the above. Trust party maybe?
      var pList = (AddonPartyList*)Subsystems.GameGui.GetAddonByName("_PartyList", 1);
      var pTypeNode = pList->PartyTypeTextNode;
      string pType = pTypeNode->NodeText.ToString();
      switch (pType)
      {
          case "Solo": 
            return 0;
          case "Party": case "Light Party": case "Full Party": 
            return 1;
          case "Cross-world Party": 
            return 2;
          case "Alliance A": case "Alliance B": case "Alliance C":
          case "Alliance D": case "Alliance E": case "Alliance F":
          case "Alliance G": case "Alliance H": case "Alliance I":
            return 3;
          default: 
            PluginLog.Debug($"FFLogsPartyLookup: Warning (Unexpected party type): {pType}");
            return -1;
      }
    }

    private static List<playerInfo> _getInfoFromSoloParty()
    {
      List<playerInfo> output = new List<playerInfo>();
      PlayerCharacter you = Subsystems.ClientState.LocalPlayer;
      string yourName = you.Name.ToString();
      string yourWorld = worldNameFromByte((byte)you.HomeWorld.Id);
      string yourRegion = regionFromWorld(yourWorld);
      playerInfo yourInfo = new playerInfo(yourName, yourWorld, yourRegion);
      output.Add(yourInfo);
      return output;
    }

    private static List<playerInfo> _getInfoFromNormalParty()
    {
      // Generates a list of playerInfo objects from the game's memory
      // assuming the party is a normal party (light/full/etc.)
      string tempName;
      string tempWorld;
      string tempRegion;
      List<playerInfo> output = new List<playerInfo>();
      int pCount = Subsystems.PartyList.Length;

      //int i=0;
      for (int i=0; i<pCount; i++)
      {
        IntPtr memberPtr = Subsystems.PartyList.GetPartyMemberAddress(i);
        PartyMember member = Subsystems.PartyList.CreatePartyMemberReference(memberPtr);
        tempName = member.Name.ToString();
        tempWorld = worldNameFromByte((byte)member.World.Id);
        tempRegion = regionFromWorld(tempWorld);
        output.Add(new playerInfo(tempName, tempWorld, tempRegion));
      }
      return output;
    }

    private static List<playerInfo> _getInfoFromCrossWorldParty()
    {
      // Generates a list of playerInfo objects from the game's memory
      // assuming the party is a cross-world party
      List<playerInfo> output = new List<playerInfo>();
      byte pSize = getCrossRealmPartySize();
      for (int i = 0; i<pSize; i++)
      {
        output.Add(GetCrossRealmPlayer(i));
      }
      return output;
    }

    private static List<playerInfo> _getInfoFromAllianceParty()
    {
      // Generates a list of playerInfo objects from the game's memory
      // assuming the party is an alliance party. Alliance parties are a 
      // bit funky though... If you're in the overworld, they work like a
      // cross world party and if you're in a duty they work like a normal
      // party. We can check the PartyList.Length attribute to figure out
      // which is which. If we're in a crossworld alliance party (overworld),
      // then PartyList.Length will be 0, otherwise it will be some non-
      // negative number. We are also assuming the party is either Alliance A,
      // Alliance B, ... etc. already.
      Subsystems.ChatGui.Print(Subsystems.PartyList.Length.ToString());
      if (Subsystems.PartyList.Length == 0) // Then we're in the overworld
      {
        return _getInfoFromCrossWorldParty();
      }
      else // Then we're in a duty
      {
        return _getInfoFromNormalParty();
      }
    }


    public static List<playerInfo> getInfoFromParty()
    {
      // This is the outward facing method you can interact with if you don't
      // want to deal with figuring out what kind of party you're in yourself.
      // Returns null if an unknown party type is detected.

      // Determine the party type
      int pType = getPartyType();
      switch (pType)
      {
        case 0: // Solo Party
        return _getInfoFromSoloParty();

        case 1: // Normal non-cross world party
        return _getInfoFromNormalParty();

        case 2: // Cross World party
        return _getInfoFromCrossWorldParty();

        case 3: // Alliance Party
        return _getInfoFromAllianceParty();

        default: // Fail
        return null;
      }
    }
  }
}

