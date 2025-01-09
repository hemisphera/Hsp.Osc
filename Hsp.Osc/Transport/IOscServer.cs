using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hsp.Osc;

public interface IOscServer : IDisposable
{
  event EventHandler<MessageReceivedEventArgs> MessageReceived;

  void BeginListen();

  void EndListen();

  void RegisterHandler(Regex re, Func<MessageHandlerContext, Task> handler);
}