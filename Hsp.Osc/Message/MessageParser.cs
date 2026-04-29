using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hsp.Osc;

public class MessageParser
{
  public static Encoding StringEncoding { get; set; } = Encoding.UTF8;

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
              case TypeTag.OscDouble:
              case TypeTag.OscInt64:
              case TypeTag.OscTimetag:
              case TypeTag.OscString:
              case TypeTag.OscSymbol:
              case TypeTag.OscBlob:
              case TypeTag.OscChar:
              case TypeTag.OscRgbaColor:
              case TypeTag.OscTrue:
              case TypeTag.OscFalse:
              case TypeTag.OscNil:
              case TypeTag.OscInfinitum:
              case TypeTag.OscArrayBegin:
              case TypeTag.OscArrayEnd:
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

        case TypeTag.OscDouble:
          var doubleVal = ParseDouble64(data, byteIndex);
          msg.PushAtom(doubleVal);
          incrementBy = 8;
          break;

        case TypeTag.OscInt64:
          var int64Val = ParseInt64(data, byteIndex);
          msg.PushAtom(int64Val);
          incrementBy = 8;
          break;

        case TypeTag.OscTimetag:
          var timetagVal = ParseUInt64(data, byteIndex);
          msg.PushAtom(timetagVal);
          incrementBy = 8;
          break;

        case TypeTag.OscString:
          var stringVal = ParseString(data, byteIndex);
          msg.PushAtom(stringVal);
          incrementBy = (StringEncoding.GetByteCount(stringVal) + 4) / 4 * 4;
          break;

        case TypeTag.OscSymbol:
          var symbolStr = ParseString(data, byteIndex);
          var symbolAtom = new Atom(TypeTag.OscSymbol) { SymbolValue = symbolStr };
          msg.Atoms.Add(symbolAtom);
          incrementBy = (StringEncoding.GetByteCount(symbolStr) + 4) / 4 * 4;
          break;

        case TypeTag.OscBlob:
          var blobVal = ParseBlob(data, byteIndex, ref incrementBy);
          msg.PushAtom(blobVal);
          break;

        case TypeTag.OscChar:
          var charRaw = ParseInt32(data, byteIndex);
          msg.PushAtom((char)(charRaw & 0xFF));
          break;

        case TypeTag.OscRgbaColor:
          var rgbaAtom = new Atom(TypeTag.OscRgbaColor) { RgbaValue = unchecked((uint)ParseInt32(data, byteIndex)) };
          msg.Atoms.Add(rgbaAtom);
          break;

        case TypeTag.OscTrue:
        case TypeTag.OscFalse:
        case TypeTag.OscNil:
        case TypeTag.OscInfinitum:
        case TypeTag.OscArrayBegin:
        case TypeTag.OscArrayEnd:
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

  private double ParseDouble64(byte[] data, int startPos)
  {
    const int incrementBy = 8;
    var tempPos = startPos;

    if (startPos + incrementBy > data.Length) throw new MalformedMessageException("Missing binary data for double64 at byte index " + startPos + ".", data);

    if (BitConverter.IsLittleEndian)
    {
      var littleEndianBytes = new byte[8];
      littleEndianBytes[7] = data[tempPos++];
      littleEndianBytes[6] = data[tempPos++];
      littleEndianBytes[5] = data[tempPos++];
      littleEndianBytes[4] = data[tempPos++];
      littleEndianBytes[3] = data[tempPos++];
      littleEndianBytes[2] = data[tempPos++];
      littleEndianBytes[1] = data[tempPos++];
      littleEndianBytes[0] = data[tempPos];

      startPos = 0;
      data = littleEndianBytes;
    }

    return BitConverter.ToDouble(data, startPos);
  }

  private static long ParseInt64(byte[] data, int startPos)
  {
    const int incrementBy = 8;
    var tempPos = startPos;

    if (startPos + incrementBy > data.Length) throw new MalformedMessageException("Missing binary data for int64 at byte index " + startPos + ".", data);

    if (BitConverter.IsLittleEndian)
    {
      var littleEndianBytes = new byte[8];
      littleEndianBytes[7] = data[tempPos++];
      littleEndianBytes[6] = data[tempPos++];
      littleEndianBytes[5] = data[tempPos++];
      littleEndianBytes[4] = data[tempPos++];
      littleEndianBytes[3] = data[tempPos++];
      littleEndianBytes[2] = data[tempPos++];
      littleEndianBytes[1] = data[tempPos++];
      littleEndianBytes[0] = data[tempPos];

      startPos = 0;
      data = littleEndianBytes;
    }

    return BitConverter.ToInt64(data, startPos);
  }

  private static ulong ParseUInt64(byte[] data, int startPos)
  {
    const int incrementBy = 8;
    var tempPos = startPos;

    if (startPos + incrementBy > data.Length) throw new MalformedMessageException("Missing binary data for uint64 at byte index " + startPos + ".", data);

    if (BitConverter.IsLittleEndian)
    {
      var littleEndianBytes = new byte[8];
      littleEndianBytes[7] = data[tempPos++];
      littleEndianBytes[6] = data[tempPos++];
      littleEndianBytes[5] = data[tempPos++];
      littleEndianBytes[4] = data[tempPos++];
      littleEndianBytes[3] = data[tempPos++];
      littleEndianBytes[2] = data[tempPos++];
      littleEndianBytes[1] = data[tempPos++];
      littleEndianBytes[0] = data[tempPos];

      startPos = 0;
      data = littleEndianBytes;
    }

    return BitConverter.ToUInt64(data, startPos);
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

    return StringEncoding.GetString(ms.ToArray()).TrimEnd('\0');
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
}