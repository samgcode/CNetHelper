
namespace Celeste.Mod.CNHExample.Data
{
  public class Death
  {
    public string map { get; set; }
    public string room { get; set; }

    public Death(string map, string room)
    {
      this.map = map;
      this.room = room;
    }
  }
}
