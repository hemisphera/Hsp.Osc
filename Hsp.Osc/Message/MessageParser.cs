using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hsp.Osc;

public class MessageParser
{
  public bool TryParseBundle(byte[] data, out Message[] messages)
  {
    messages = [];
    if (data.Length < 7) return false;

    using var s = new MemoryStream(data);
    var buf = new byte[8];

    // #bundle identifier
    _ = s.Read(buf, 0, buf.Length);
    if (Encoding.ASCII.GetString(buf[..7]) != "#bundle") return false;

    // timetag
    _ = s.Read(buf, 0, buf.Length);

    // messages
    var messageList = new List<Message>();
    using var reader = new BinaryReader(s);
    while (s.Position != s.Length)
    {
      var nextBlockLength = ReadInt32(s);
      var buffer = new byte[nextBlockLength];
      _ = reader.Read(buffer, 0, buffer.Length);
      messageList.Add(Parse(buffer));
    }

    messages = messageList.ToArray();
    return true;
  }

  public Message Parse(byte[] data)
  {
    string? address = null;
    try
    {
      var byteIndex = 0;
      var typeTags = new List<TypeTag>();
      byteIndex = ParseAddress(data, out address, byteIndex);
      var msg = new Message(address);
      byteIndex = ParseTypeTags(data, typeTags, byteIndex);
      ParseMessageData(data, typeTags, msg, byteIndex);
      return msg;
    }
    catch (Exception ex)
    {
      throw new MalformedMessageException("Unable to parse message.", data, ex, address);
    }
  }

  public byte[] Parse(Message message)
  {
    var builder = new List<byte>();

    SerializeAddress(message, builder);
    SerializeTypeTags(message, builder);
    SerializeMessageData(message, builder);

    return builder.ToArray();
  }

  private static int ParseAddress(byte[] data, out string address, int startIndex)
  {
    var addressBuilder = new StringBuilder();
    var byteIndex = startIndex;

    while (byteIndex < data.Length)
    {
      var hasNull = false;

      for (var i = 0; i < 4; i++)
      {
        var val = data[byteIndex];

        if (val > byte.MinValue)
        {
          if (hasNull) throw new MalformedMessageException("Invalid address: address data appearing after null padding at byte position " + byteIndex + ".", data);

          if (val == (byte)',') throw new MalformedMessageException("Invalid address: no null padding before typetags begin at byte position " + byteIndex + ".", data);

          addressBuilder.Append((char)val);
        }
        else
        {
          hasNull = true;
        }

        byteIndex++;
      }

      if (hasNull) break;
    }

    address = addressBuilder.ToString(); // throws if address is invalid
    if (address == null) throw new ArgumentNullException(nameof(address));
    if (address.Length == 0) throw new ArgumentException("address cannot be empty.", nameof(address));
    if (address[0] != '/')
      throw new ArgumentException("address must begin with a forward-slash ('/').", nameof(address));
    return byteIndex;
  }

  private static int ParseTypeTags(byte[] data, List<TypeTag> typeTags, int startIndex)
  {
    var byteIndex = startIndex;

    while (byteIndex < data.Length && data[byteIndex] != ',')
      byteIndex++;

    while (byteIndex < data.Length)
    {
      var hasNull = false;

      for (var i = 0; i < 4; i++)
      {
        var val = data[byteIndex];

        if (data[byteIndex] != ',')
        {
          if (val > byte.MinValue)
          {
            if (hasNull) throw new MalformedMessageException("Invalid type tags: type tag data appearing after null padding at byte pos + " + byteIndex + ".", data);

            var typeTag = (TypeTag)val;

            switch (typeTag)
            {
              case TypeTag.OscInt32:
              case TypeTag.OscFloat32:
              case TypeTag.OscString:
              case TypeTag.OscBlob:
              case TypeTag.OscTrue:
              case TypeTag.OscFalse:
              case TypeTag.OscNil:
                typeTags.Add(new Atom(typeTag));
                break;

              default:
                throw new MalformedMessageException("Unknown/invalid type tag " + typeTag + " at byte pos " + byteIndex + ".", data);
            }
          }
          else
          {
            hasNull = true;
          }
        }

        byteIndex++;
      }

      if (hasNull) break;
    }

    return byteIndex;
  }

  private void ParseMessageData(byte[] data, List<TypeTag> tags, Message msg, int startIndex)
  {
    var byteIndex = startIndex;
    for (var tagIndex = 0; tagIndex < tags.Count; tagIndex++)
    {
      var canIncrement = true;
      var incrementBy = 4;

      var tag = tags[tagIndex];
      switch (tag)
      {
        case TypeTag.OscInt32:
          var intVal = ParseInt32(data, byteIndex);
          msg.PushAtom(intVal);
          break;

        case TypeTag.OscFloat32:
          var floatVal = ParseFloat32(data, byteIndex);
          msg.PushAtom(floatVal);
          break;

        case TypeTag.OscString:
          var stringVal = ParseString(data, byteIndex);
          msg.PushAtom(stringVal);
          break;

        case TypeTag.OscBlob:
          var blobVal = ParseBlob(data, byteIndex, ref incrementBy);
          msg.PushAtom(blobVal);
          break;

        case TypeTag.OscNil:
          canIncrement = false;
          break;
      }

      if (canIncrement)
        byteIndex += incrementBy;
    }
  }

  private static int ReadInt32(MemoryStream s)
  {
    var buf = new byte[4];
    _ = s.Read(buf, 0, buf.Length);
    return ParseInt32(buf, 0);
  }

  private static int ParseInt32(byte[] data, int startPos)
  {
    const int incrementBy = 4;
    var tempPos = startPos;

    if (startPos + incrementBy > data.Length) throw new MalformedMessageException("Missing binary data for int32 at byte index " + startPos + ".", data);

    if (BitConverter.IsLittleEndian)
    {
      var littleEndianBytes = new byte[4];
      littleEndianBytes[3] = data[tempPos++];
      littleEndianBytes[2] = data[tempPos++];
      littleEndianBytes[1] = data[tempPos++];
      littleEndianBytes[0] = data[tempPos];

      startPos = 0;
      data = littleEndianBytes;
    }

    return BitConverter.ToInt32(data, startPos);
  }

  private float ParseFloat32(byte[] data, int startPos)
  {
    const int incrementBy = 4;
    var tempPos = startPos;

    if (startPos + incrementBy > data.Length) throw new MalformedMessageException("Missing binary data for float32 at byte index " + startPos + ".", data);

    if (BitConverter.IsLittleEndian)
    {
      var littleEndianBytes = new byte[4];
      littleEndianBytes[3] = data[tempPos++];
      littleEndianBytes[2] = data[tempPos++];
      littleEndianBytes[1] = data[tempPos++];
      littleEndianBytes[0] = data[tempPos];

      startPos = 0;
      data = littleEndianBytes;
    }

    return BitConverter.ToSingle(data, startPos);
  }

  private static string ParseString(byte[] data, int startPos)
  {
    if (startPos + 4 > data.Length)
      throw new MalformedMessageException("Missing binary data for string atom at byte index " + startPos + ".", data);

    using var ms = new MemoryStream();
    do
    {
      ms.Write(data, startPos, 4);
      startPos += 4;
    } while (data[startPos - 1] != 0);

    return Encoding.ASCII.GetString(ms.ToArray()).TrimEnd('\0');
  }

  private static byte[] ParseBlob(byte[] data, int startPos, ref int incrementBy)
  {
    var length = ParseInt32(data, startPos);

    if (length < 0) throw new MalformedMessageException("Invalid blob length at byte index " + startPos + ".", data);

    startPos += 4;
    incrementBy = 4;

    if (data[startPos] == byte.MinValue)
    {
      startPos += 4;
      incrementBy += 4;
    }

    incrementBy += length;
    incrementBy += 4 - incrementBy % 4;

    if (startPos > data.Length) throw new MalformedMessageException("Missing binary data for blob atom at byte index " + startPos + ".", data);

    var blob = new byte[length];
    Array.Copy(data, startPos, blob, 0, length);
    return blob;
  }

  private static void SerializeAddress(Message message, List<byte> builder)
  {
    var count = message.Address.Length;
    builder.AddRange(Encoding.ASCII.GetBytes(message.Address));

    builder.Add(byte.MinValue);
    count++;

    while (count++ % 4 != 0) builder.Add(byte.MinValue);
  }

  private static void SerializeTypeTags(Message message, List<byte> builder)
  {
    var count = 0;

    builder.Add((byte)',');
    count++;

    foreach (var typetag in message.TypeTags)
    {
      builder.Add((byte)typetag);
      count++;
    }

    builder.Add(byte.MinValue);
    count++;

    while (count++ % 4 != 0) builder.Add(byte.MinValue);
  }

  private void SerializeMessageData(Message message, List<byte> builder)
  {
    foreach (var atom in message)
    {
      var type = atom.TypeTag;

      switch (type)
      {
        case TypeTag.OscInt32:
          SerializeInt32(atom.Int32Value, builder);
          break;

        case TypeTag.OscFloat32:
          SerializeFloat32(atom.Float32Value, builder);
          break;

        case TypeTag.OscString:
          SerializeString(atom.StringValue, builder);
          break;

        case TypeTag.OscBlob:
          SerializeBlob(atom.BlobValue, builder);
          break;
      }
    }
  }

  private static void SerializeInt32(int value, List<byte> builder)
  {
    var bytes = BitConverter.GetBytes(value);

    if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

    builder.AddRange(bytes);
  }

  private static void SerializeFloat32(float value, List<byte> builder)
  {
    var bytes = BitConverter.GetBytes(value);

    if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

    builder.AddRange(bytes);
  }

  private static void SerializeString(string? value, List<byte> builder)
  {
    SerializeBlob(Encoding.ASCII.GetBytes(value ?? string.Empty), builder);
  }

  private static void SerializeBlob(byte[]? value, List<byte> builder)
  {
    SerializeInt32(value?.Length ?? 0, builder);

    builder.AddRange(value ?? []);
    builder.Add(byte.MinValue);

    var temp = value?.Length ?? 0 + 1;
    while (temp++ % 4 != 0) builder.Add(byte.MinValue);
  }
}