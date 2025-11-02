using Celeste.Mod.CelesteNet.DataTypes;

namespace Celeste.Mod.CNetHelper.Data;

public class PlayerData
{
  public uint ID;
  public string Name = "";
  public string FullName = "";
  public string DisplayName = "";

  public PlayerData(DataPlayerInfo playerInfo)
  {
    ID = playerInfo.ID;
    Name = playerInfo.Name;
    FullName = playerInfo.FullName;
    DisplayName = playerInfo.DisplayName;
  }
}
