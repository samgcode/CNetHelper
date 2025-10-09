using System;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CNetHelper.Data;
using Celeste.Mod.CNetHelper.IO;

namespace Celeste.Mod.CNetHelper;

public class CNetHelperModule : EverestModule
{
    public static CNetHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(CNetHelperModuleSettings);
    public static CNetHelperModuleSettings Settings => (CNetHelperModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(CNetHelperModuleSession);
    public static CNetHelperModuleSession Session => (CNetHelperModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(CNetHelperModuleSaveData);
    public static CNetHelperModuleSaveData SaveData => (CNetHelperModuleSaveData)Instance._SaveData;

    public static CNetComm Comm;

    public static event CNetComm.OnErrorHandler OnError;

    public CNetHelperModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(CNetHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(CNetHelperModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        Celeste.Instance.Components.Add(Comm = new CNetComm(Celeste.Instance));
        CNetComm.OnError += OnErrorInternal;
    }

    public override void Unload()
    {
        CNetComm.OnError -= OnErrorInternal;
        Celeste.Instance.Components.Remove(Comm);
        Comm = null;
    }

    public static void Send<T>(T data, bool sendToSelf = false)
    {
        Comm.Send(data, sendToSelf);
    }

    public static void RegisterType<T>(Action<DataPlayerInfo, T> handler)
    {
        CNetComm.RegisterType(handler);
    }


    public static void OnErrorInternal(CNetHelperError error)
    {
        OnError?.Invoke(error);
    }
}
