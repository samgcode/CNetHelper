using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Components;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CNetHelper.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Celeste.Mod.CNetHelper.IO
{
  public class CNetComm : GameComponent
  {
    public static CNetComm Instance { get; private set; }

    private static Dictionary<string, Type> registeredTypes = new Dictionary<string, Type>();
    private static Dictionary<Type, Delegate> registeredHandlers = new Dictionary<Type, Delegate>();

    #region Events

    public delegate void OnConnectedHandler(CelesteNetClientContext cxt);
    internal static event OnConnectedHandler OnConnected;

    public delegate void OnReceiveConnectionInfoHandler(DataConnectionInfo data);
    internal static event OnReceiveConnectionInfoHandler OnReceiveConnectionInfo;

    public delegate void OnDisonnectedHandler(CelesteNetConnection con);
    internal static event OnDisonnectedHandler OnDisconnected;

    public delegate void OnReceiveUpdateHandler(DataPlayerInfo player, object data);

    public delegate void OnErrorHandler(CNetHelperError error);
    internal static event OnErrorHandler OnError;

    #endregion

    #region Local State Information

    public CelesteNetClientContext CnetContext { get { return CelesteNetClientModule.Instance?.Context; } }

    public CelesteNetClient CnetClient { get { return CelesteNetClientModule.Instance?.Client; } }
    public bool IsConnected { get { return CnetClient?.Con?.IsConnected ?? false; } }
    public uint? CnetID { get { return IsConnected ? (uint?)CnetClient?.PlayerInfo?.ID : null; } }
    public long MaxPacketSize { get { return CnetClient?.Con is CelesteNetTCPUDPConnection connection ? (connection.ConnectionSettings?.MaxPacketSize ?? 2048) : 2048; } }

    public DataChannelList.Channel CurrentChannel
    {
      get
      {
        if (!IsConnected) return null;
        KeyValuePair<Type, CelesteNetGameComponent> listComp = CnetContext.Components.FirstOrDefault((KeyValuePair<Type, CelesteNetGameComponent> kvp) =>
        {
          return kvp.Key == typeof(CelesteNetPlayerListComponent);
        });
        if (listComp.Equals(default(KeyValuePair<Type, CelesteNetGameComponent>))) return null;
        CelesteNetPlayerListComponent comp = listComp.Value as CelesteNetPlayerListComponent;
        DataChannelList.Channel[] list = comp.Channels?.List;
        return list?.FirstOrDefault(c => c.Players.Contains(CnetClient.PlayerInfo.ID));
      }
    }
    public bool CurrentChannelIsMain
    {
      get
      {
        return CurrentChannel?.Name?.ToLower() == "main";
      }
    }

    public bool CanSendMessages
    {
      get
      {
        return IsConnected;
      }
    }

    private ConcurrentQueue<Action> updateQueue = new ConcurrentQueue<Action>();

    #endregion

    #region Setup

    public CNetComm(Game game)
      : base(game)
    {
      Instance = this;
      Disposed += OnComponentDisposed;
      CelesteNetClientContext.OnStart += OnCNetClientContextStart;
      CelesteNetClientContext.OnDispose += OnCNetClientContextDispose;
    }

    private void OnComponentDisposed(object sender, EventArgs args)
    {
      CelesteNetClientContext.OnStart -= OnCNetClientContextStart;
      CelesteNetClientContext.OnDispose -= OnCNetClientContextDispose;
    }

    #endregion

    #region Hooks + Events

    private void OnCNetClientContextStart(CelesteNetClientContext cxt)
    {
      CnetClient.Data.RegisterHandlersIn(this);
      CnetClient.Con.OnDisconnect += OnDisconnect;
      updateQueue.Enqueue(() => OnConnected?.Invoke(cxt));
    }

    private void OnCNetClientContextDispose(CelesteNetClientContext cxt)
    {
      // CnetClient is null here
    }

    private void OnDisconnect(CelesteNetConnection con)
    {
      updateQueue.Enqueue(() => OnDisconnected?.Invoke(con));
    }

    public override void Update(GameTime gameTime)
    {
      ConcurrentQueue<Action> queue = updateQueue;
      updateQueue = new ConcurrentQueue<Action>();
      foreach (Action act in queue)
      {
        act();
      }

      base.Update(gameTime);
    }

    #endregion

    internal static void RegisterType<T>(Action<DataPlayerInfo, T> handler)
    {
      string type_key = typeof(T).ToString();
      if (registeredTypes.ContainsKey(type_key))
      {
        registeredTypes[type_key] = typeof(T);
        registeredHandlers[typeof(T)] = handler;
      }
      else
      {
        registeredTypes.Add(type_key, typeof(T));
        registeredHandlers.Add(typeof(T), handler);
      }
    }

    #region Entry Points

    public void Send<T>(T data, bool sendToSelf)
    {
      if (!CanSendMessages)
      {
        return;
      }
      try
      {
        string type_key = typeof(T).ToString();
        if (!registeredTypes.ContainsKey(type_key) || !registeredHandlers.ContainsKey(data.GetType()))
        {
          OnError?.Invoke(new CNetHelperError("Send", $"Type {type_key} not registered", null));
          return;
        }

        string json = JsonSerializer.Serialize(data);
        CNetUpdate update = new CNetUpdate(type_key, json);

        if (sendToSelf) CnetClient.SendAndHandle(update);
        else CnetClient.Send(update);
      }
      catch (Exception e)
      {
        // "The only way I know of for this to happen is a well-timed connection blorp but just in case" -corkr900
        Logger.Log(LogLevel.Error, "CNetHelper", $"Exception was handled in CNetHelper.IO.CNetComm.Send<{typeof(T).Name}>");
        Logger.LogDetailed(LogLevel.Error, "CNetHelper", e.Message);
      }
    }

    #endregion

    #region Message Handlers

    public void Handle(CelesteNetConnection con, DataConnectionInfo data)
    {
      if (data.Player == null) data.Player = CnetClient.PlayerInfo;  // It's null when handling our own messages
      updateQueue.Enqueue(() => OnReceiveConnectionInfo?.Invoke(data));
    }

    public void Handle(CelesteNetConnection con, CNetUpdate data)
    {
      if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages

      Type type = registeredTypes[data.typeKey];
      if (type == null)
      {
        OnError?.Invoke(new CNetHelperError("Handle", $"Type {data.typeKey} not registered", null));
        return;
      }

      object obj;
      try
      {
        obj = JsonSerializer.Deserialize(data.json, type);
      }
      catch
      {
        OnError?.Invoke(new CNetHelperError("Handle", $"Failed to deserialize {data.typeKey}", null));
        return;
      }


      Delegate handler = registeredHandlers[type];
      updateQueue.Enqueue(() => handler.DynamicInvoke(data.player, Convert.ChangeType(obj, type)));
    }

    #endregion
  }
}
