using System;
using System.Reflection;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CNetHelper;
using Celeste.Mod.CNetHelper.Data;
using Celeste.Mod.CNHExample.Data;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.CNHExample;

public class CNHExampleModule : EverestModule
{
    public static CNHExampleModule Instance { get; private set; }

    public override Type SettingsType => typeof(CNHExampleModuleSettings);
    public static CNHExampleModuleSettings Settings => (CNHExampleModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(CNHExampleModuleSession);
    public static CNHExampleModuleSession Session => (CNHExampleModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(CNHExampleModuleSaveData);
    public static CNHExampleModuleSaveData SaveData => (CNHExampleModuleSaveData)Instance._SaveData;

    private static Hook hook_Player_orig_Die;

    private static bool propagate = true;
    private static string map;
    private static string room;

    public CNHExampleModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(CNHExampleModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(CNHExampleModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        Logger.Log(LogLevel.Info, "CNHExampleTest", "Loaded CNHExampleTestModule");

        CNetHelperModule.RegisterType<Death>(OnReceiveDeath);
        CNetHelperModule.OnError += OnError;

        hook_Player_orig_Die = new Hook(
               typeof(Player).GetMethod("orig_Die", BindingFlags.Public | BindingFlags.Instance),
               typeof(CNHExampleModule).GetMethod("OnPlayerDie"));

        On.Celeste.LevelLoader.StartLevel += OnLoadLevel;
        On.Celeste.Player.OnTransition += OnPlayerTransition;
    }

    public override void Unload()
    {
        CNetHelperModule.OnError -= OnError;

        hook_Player_orig_Die?.Dispose();
        hook_Player_orig_Die = null;

        On.Celeste.LevelLoader.StartLevel -= OnLoadLevel;
        On.Celeste.Player.OnTransition -= OnPlayerTransition;
    }

    public static void OnError(CNetHelperError error)
    {
        Logger.Log(LogLevel.Error, "CNHExampleTest", $"Error occured in {error.location}: {error.message}");
    }

    public static void OnLoadLevel(On.Celeste.LevelLoader.orig_StartLevel orig, LevelLoader self)
    {
        map = self.Level.Session.Area.SID;
        room = self.Level.Session.Level;
        orig(self);
    }

    public static void OnPlayerTransition(On.Celeste.Player.orig_OnTransition orig, Player self)
    {
        Session session = self.SceneAs<Level>().Session;
        map = session.Area.SID;
        room = session.Level;
        orig(self);
    }

    public static PlayerDeadBody OnPlayerDie(Func<Player, Vector2, bool, bool, PlayerDeadBody> orig, Player self, Vector2 direction, bool ifInvincible, bool registerStats)
    {
        Logger.Log(LogLevel.Info, "CNHExampleTest", $"Player died in {map}, {room}");
        if (propagate)
        {
            Logger.Log(LogLevel.Info, "CNHExampleTest", $"Player died in {map}, {room}");
            CNetHelperModule.Send(new Death(map, room), true);
        }
        propagate = true;

        // Now actually do the thing
        return orig(self, direction, ifInvincible, registerStats);
    }


    private static void OnReceiveDeath(DataPlayerInfo playerInfo, Death update)
    {
        Logger.Log(LogLevel.Info, "CNHExampleTest", $"Received update from {playerInfo.FullName}: {update.map}, {update.room}");

        propagate = false;
        Player player = Engine.Scene.Tracker.GetEntity<Player>();
        if (player != null)
        {
            if (player.StateMachine.State != Player.StDummy)
            {
                player.Die(Vector2.Zero);
            }
            else
            {
                Logger.Log(LogLevel.Debug, "Deathlink", "Player not found");
            }
        }
    }
}
