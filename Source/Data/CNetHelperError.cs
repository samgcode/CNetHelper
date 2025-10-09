

using System;

namespace Celeste.Mod.CNetHelper.Data
{
  public class CNetHelperError
  {
    public string message { get; set; }
    public string location { get; set; }
    public Exception exception { get; set; }

    public CNetHelperError(string location, string message, Exception exception)
    {
      this.message = message;
      this.location = location;
      this.exception = exception;
    }
  }
}
