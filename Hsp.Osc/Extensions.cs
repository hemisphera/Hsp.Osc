using System.Threading.Tasks;

namespace Hsp.Osc;

public static class Extensions
{
  public static async Task Send(this IMessage msg, IOscClient client)
  {
    await client.SendMessageAsync(msg);
  }
}