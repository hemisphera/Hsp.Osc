using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hsp.Osc;

public class MessageHandlerContext
{
  public Message Message { get; }

  public Match Match { get; }

  public MessageHandler Handler { get; }


  internal MessageHandlerContext(Message message, Match match, MessageHandler handler)
  {
    Message = message;
    Match = match;
    Handler = handler;
  }


  public int GetAddressValueAsInt(string name)
  {
    return int.Parse(GetAddressValueAsString(name));
  }

  public string GetAddressValueAsString(string name)
  {
    return Match.Groups[name].Value;
  }


  public async Task ExecuteHandler()
  {
    await Handler.Handler(this);
  }
}