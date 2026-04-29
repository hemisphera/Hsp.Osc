using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hsp.Osc;

public sealed class Message : IEnumerable<Atom>, IMessage
{
  public string Address { get; }
  public TypeTag[] TypeTags => Atoms.Select(a => a.TypeTag).ToArray();
  public List<Atom> Atoms { get; } = [];


  public Message(string address)
  {
    Address = address;
  }

  /*
  public override bool Equals(object? obj)
  {
    return Equals(obj as Message);
  }

  public bool Equals(Message? rhs)
  {
    if (rhs == null) return false;

    if (Address != rhs.Address) return false;
    if (Atoms == null)
      return rhs.Atoms == null;
    if (rhs.Atoms == null) return false;

    if (Atoms.Count != rhs.Atoms.Count) return false;

    for (var i = 0; i < Atoms.Count; i++)
      if (!Atoms[i].Equals(rhs.Atoms[i]))
        return false;

    return true;
  }

  public override int GetHashCode()
  {
    var hashCode = Address == null ? 0 : Address.GetHashCode();
    if (Atoms != null)
      foreach (var atom in Atoms)
        hashCode ^= atom.GetHashCode();

    return hashCode;
  }
  */

  public override string ToString()
  {
    return $"{Address} -- {string.Join(" ", this)}";
  }


  public Message PushAtom(int value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(byte[] value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(string value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(double value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(long value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(ulong value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(char value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(float value)
  {
    Atoms.Add(new Atom(value));
    return this;
  }

  public Message PushAtom(IEnumerable<Atom> atoms)
  {
    Atoms.AddRange(atoms);
    return this;
  }

  public byte[] ToBytes()
  {
    var builder = new List<byte>();
    SerializeAddress(this, builder);
    SerializeTypeTags(this, builder);
    SerializeMessageData(this, builder);
    return builder.ToArray();
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

  private static void SerializeMessageData(Message message, List<byte> builder)
  {
    foreach (var atom in message)
    {
      switch (atom.TypeTag)
      {
        case TypeTag.OscInt32:
          SerializeInt32(atom.Int32Value, builder);
          break;
        case TypeTag.OscFloat32:
          SerializeFloat32(atom.Float32Value, builder);
          break;
        case TypeTag.OscDouble:
          SerializeDouble64(atom.Double64Value, builder);
          break;
        case TypeTag.OscInt64:
          SerializeInt64(atom.Int64Value, builder);
          break;
        case TypeTag.OscTimetag:
          SerializeUInt64(atom.TimetagValue, builder);
          break;
        case TypeTag.OscString:
          SerializeString(atom.StringValue, builder);
          break;
        case TypeTag.OscSymbol:
          SerializeString(atom.SymbolValue, builder);
          break;
        case TypeTag.OscChar:
          SerializeInt32(atom.CharValue, builder);
          break;
        case TypeTag.OscRgbaColor:
          SerializeInt32(unchecked((int)atom.RgbaValue), builder);
          break;
        case TypeTag.OscBlob:
          SerializeBlob(atom.BlobValue, builder);
          break;
        // OscTrue, OscFalse, OscNil, OscInfinitum, OscArrayBegin, OscArrayEnd: no data bytes
      }
    }
  }

  internal static void SerializeInt32(int value, List<byte> builder)
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

  private static void SerializeDouble64(double value, List<byte> builder)
  {
    var bytes = BitConverter.GetBytes(value);
    if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
    builder.AddRange(bytes);
  }

  private static void SerializeInt64(long value, List<byte> builder)
  {
    var bytes = BitConverter.GetBytes(value);
    if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
    builder.AddRange(bytes);
  }

  private static void SerializeUInt64(ulong value, List<byte> builder)
  {
    var bytes = BitConverter.GetBytes(value);
    if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
    builder.AddRange(bytes);
  }

  private static void SerializeString(string? value, List<byte> builder)
  {
    var bytes = MessageParser.StringEncoding.GetBytes(value ?? string.Empty);
    builder.AddRange(bytes);
    var count = bytes.Length;
    builder.Add(byte.MinValue);
    count++;
    while (count++ % 4 != 0) builder.Add(byte.MinValue);
  }

  private static void SerializeBlob(byte[]? value, List<byte> builder)
  {
    SerializeInt32(value?.Length ?? 0, builder);
    builder.AddRange(value ?? []);
    builder.Add(byte.MinValue);
    var temp = (value?.Length ?? 0) + 1;
    while (temp++ % 4 != 0) builder.Add(byte.MinValue);
  }


  public IEnumerator<Atom> GetEnumerator()
  {
    foreach (var atom in Atoms ?? []) yield return atom;
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}