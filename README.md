# CNetHelper

CNetHelper is a helper that assists with making mods utilizing CelesteNet. It allows for a simple API for making mods that communicate over CelesteNet.

A full example mod which is a minimal implementation of [Deathlink](https://gamebanana.com/mods/546779) is available in this repository [Here](https://github.com/samgcode/CNetHelper/tree/main/Example)

The basic things required to use this API are the following:

The first thing is to add an error handler:

```CS
public override void Load() {
  CNetHelperModule.OnError += OnError;
}

public override void Unload() {
  CNetHelperModule.OnError -= OnError;
}

public static void OnError(CNetHelperError error) {
  Logger.Log(LogLevel.Error, "CNHExample", $"Error occured in {error.location}: {error.message}");
}
```

Then to send messages, create a class that is JSON serializable to represent the date you are sending:

```CS
public class Message {
  public string text { get; set; }

  public Message(string text_to_send) {
    this.text = text_to_send;
  }
}
```

Then create and register the message received handler:

```CS
public override void Load()
{
  CNetHelperModule.OnError += OnError;
  CNetHelperModule.RegisterType<Message>(OnReceiveMessage);
}

private static void OnReceiveMessage(DataPlayerInfo playerInfo, Message msg) {
  Logger.Log(LogLevel.Info, "CNHExample", $"Received a message from {playerInfo.FullName}: {msg.text}");
}
```

And finally you can send an instance of a message with the Send method:
```CS
CNetHelperModule.Send(new Message("haiii mrrrow :3"), false); // set to true if you want the current client to receive this message also
```
