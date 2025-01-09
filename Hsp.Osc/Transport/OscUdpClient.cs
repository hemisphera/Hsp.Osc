using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Hsp.Osc;

public sealed class OscUdpClient : IOscClient
{
  private static readonly MessageParser DefaultMessageParser = new();
  private readonly Socket _socket;

  public IPAddress Address { get; }

  public int Port { get; }


  public OscUdpClient(IPAddress address, int port)
  {
    Address = address;
    Port = port;
    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
  }


  public async Task ConnectAsync()
  {
    if (!_socket.Connected)
      await _socket.ConnectAsync(Address, Port);
  }

  public async Task DisconnectAsync()
  {
    try
    {
      if (_socket.Connected)
        await _socket.DisconnectAsync(true);
    }
    catch (SocketException)
    {
      // ignore
    }
  }

  public async Task SendMessageAsync(Message message)
  {
    var bytes = DefaultMessageParser.Parse(message);
    await _socket.SendAsync(bytes);
  }

  public void Dispose()
  {
    _socket.Dispose();
  }
}