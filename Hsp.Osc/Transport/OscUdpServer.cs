using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hsp.Osc;

public sealed class OscUdpServer : IOscServer
{
  private static readonly MessageParser DefaultMessageParser = new();
  private readonly ConcurrentBag<MessageHandler> _handlers = [];
  private readonly UdpClient _client;
  private CancellationTokenSource _token;
  private bool _islistening;

  public IPAddress Address { get; }

  public int Port { get; }


  public OscUdpServer(IPAddress address, int port)
  {
    Address = address;
    Port = port;
    _client = new UdpClient(new IPEndPoint(Address, Port));
  }


  public event EventHandler<MessageReceivedEventArgs> MessageReceived;

  public void BeginListen()
  {
    if (_islistening) return;
    _islistening = true;
    _token = new CancellationTokenSource();
    Task.Run(async () => await ListenLoop());
  }

  public void EndListen()
  {
    if (!_islistening) return;
    _token.Cancel();
    _islistening = false;
  }

  public void RegisterHandler(string pattern, Func<MessageHandlerContext, Task> handler)
  {
    RegisterHandler(new Regex(pattern), handler);
  }

  public void RegisterHandler(string pattern, Action<MessageHandlerContext> handler)
  {
    RegisterHandler(pattern, async c => await Task.Run(() => handler(c)));
  }

  public void RegisterHandler(Regex re, Func<MessageHandlerContext, Task> handler)
  {
    _handlers.Add(new MessageHandler(re, handler));
  }

  public void RegisterHandler(Regex re, Action<MessageHandlerContext> handler)
  {
    RegisterHandler(re, async c => await Task.Run(() => handler(c)));
  }

  public void RegisterHandlers(object item)
  {
    foreach (var method in item.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
    {
      var attr = method.GetCustomAttribute<OscMessageHandlerAttribute>();
      if (attr == null) continue;
      RegisterHandler(attr.Pattern, async e =>
      {
        var r = method.Invoke(item, [e]);
        if (r is Task t) await t;
      });
    }
  }


  public void Dispose()
  {
    EndListen();
    _client.Dispose();
  }

  public event EventHandler<Exception> MessageFailed;

  private async Task ListenLoop()
  {
    while (!_token.IsCancellationRequested)
      try
      {
        var data = await _client.ReceiveAsync();
        var messages =
          DefaultMessageParser.TryParseBundle(data.Buffer, out var bundleMessages)
            ? bundleMessages
            : [DefaultMessageParser.Parse(data.Buffer)];
        foreach (var msg in messages)
        {
          MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
          _ = HandleMessage(msg);
        }
      }
      catch (MalformedMessageException ex)
      {
        MessageFailed?.Invoke(this, ex);
      }
  }

  private async Task HandleMessage(Message msg)
  {
    var contexts = _handlers.Select(handler =>
    {
      var match = handler.Regex.Match(msg.Address);
      return match.Success ? new MessageHandlerContext(msg, match, handler) : null;
    }).Where(c => c != null).ToArray();
    await Task.WhenAll(contexts.Select(context => context.ExecuteHandler()));
    //if (contexts.Length == 0)
    //  Console.WriteLine(msg);
  }
}