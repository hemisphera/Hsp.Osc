using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Hsp.Osc;

public sealed class MessageBundle : IEnumerable<Message>, IMessage
{
  private readonly List<Message> _messages = [];

  /// <summary>
  /// OSC timetag. Defaults to 1 (meaning "immediately" per the OSC spec).
  /// </summary>
  public ulong Timetag { get; set; } = 1;


  public MessageBundle(params Message[] message)
  {
    _messages.AddRange(message);
  }


  public void Add(Message message)
  {
    _messages.Add(message);
  }

  public byte[] ToBytes()
  {
    var builder = new List<byte>();

    // "#bundle\0" identifier (8 bytes)
    builder.AddRange(Encoding.ASCII.GetBytes("#bundle"));
    builder.Add(0);

    // timetag (8 bytes, big-endian)
    var timetagBytes = BitConverter.GetBytes(Timetag);
    if (BitConverter.IsLittleEndian) Array.Reverse(timetagBytes);
    builder.AddRange(timetagBytes);

    // messages: 4-byte length prefix + message bytes
    foreach (var message in _messages)
    {
      var bytes = message.ToBytes();
      Message.SerializeInt32(bytes.Length, builder);
      builder.AddRange(bytes);
    }

    return builder.ToArray();
  }

  public IEnumerator<Message> GetEnumerator() => _messages.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}