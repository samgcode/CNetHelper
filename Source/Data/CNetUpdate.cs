using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CNetHelper.IO;

namespace Celeste.Mod.CNetHelper.Data
{
  public class CNetUpdate : DataType<CNetUpdate>
  {
    public DataPlayerInfo player;
    public string typeKey;
    public string json;

    static CNetUpdate()
    {
      DataID = "CNet_Update";
    }

    public CNetUpdate() : this("", "")
    {

    }

    public CNetUpdate(string type_key, string data)
    {
      this.typeKey = type_key;
      this.json = data;
    }

    public override DataFlags DataFlags => DataFlags.CoreType;


    public override MetaType[] GenerateMeta(DataContext ctx)
      => new MetaType[] {
        new MetaPlayerPrivateState(player),
      };

    public override void FixupMeta(DataContext ctx)
    {
      player = Get<MetaPlayerPrivateState>(ctx);
    }

    protected override void Read(CelesteNetBinaryReader reader)
    {
      typeKey = reader.ReadString();
      json = reader.ReadString();
    }

    protected override void Write(CelesteNetBinaryWriter writer)
    {
      writer.WriteNetString(typeKey);
      writer.WriteNetString(json);
    }

    public override string ToString()
      => $"Update json: \n {json}";
  }
}
