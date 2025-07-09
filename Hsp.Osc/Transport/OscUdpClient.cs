using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Hsp.Osc;

public sealed class OscUdpClient : IOscClient
{
  private static readonly MessageParser DefaultMessageParser = new();

  private UdpClient? _client;

  public IPAddress Address { get; }

  public int LocalPort { get; }

  public int Port { get; }


  private bool _connected;


  public OscUdpClient(IPAddress address, int port)
    : this(port, address, port)
  {
  }

  public OscUdpClient(int localPort, IPAddress address, int port)
  {
    Address = address;
    Port = port;
    LocalPort = localPort;
  }


  public async Task ConnectAsync()
  {
    if (_connected)
      await DisconnectAsync();

    _client = new UdpClient(LocalPort);
    _client.Connect(Address, Port);
    _connected = true;
  }

  public async Task DisconnectAsync()
  {
    if (!_connected) return;
    _client?.Dispose();
    _connected = false;
    await Task.CompletedTask;
  }

  public async Task SendMessageAsync(Message message)
  {
    if (_client == null) throw new Exception("Client not connected.");
    var bytes = DefaultMessageParser.Parse(message);
    await _client.SendAsync(bytes);
  }

  public void Dispose()
  {
    _client?.Dispose();
  }
}