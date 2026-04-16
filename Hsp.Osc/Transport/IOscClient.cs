using System;
using System.Threading.Tasks;

namespace Hsp.Osc;

public interface IOscClient : IDisposable
{
  Task ConnectAsync();

  Task DisconnectAsync();

  Task SendMessageAsync(IMessage message);
}