using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Hsp.Osc;

// Note that we can only overlap pure value types to get a C-style "union" type.
// The string/byte[]/"ref type" objects must live the objvalue field at offset 8.
[StructLayout(LayoutKind.Explicit)]
public struct Atom
{
  [FieldOffset(4)]
  private int int32value;

  [FieldOffset(4)]
  private float float32value;

  [FieldOffset(8)]
  private object? objvalue;

  // Properties
  [field: FieldOffset(0)]
  public TypeTag TypeTag { get; private set; }

  public int Int32Value
  {
    get => int32value;
    set
    {
      int32value = value;
      TypeTag = TypeTag.OscInt32;
    }
  }

  public float Float32Value
  {
    get => float32value;
    set
    {
      float32value = value;
      TypeTag = TypeTag.OscFloat32;
    }
  }

  public string? StringValue
  {
    get => objvalue as string;
    set
    {
      objvalue = value;
      TypeTag = TypeTag.OscString;
    }
  }

  public byte[]? BlobValue
  {
    get => objvalue as byte[];
    set
    {
      objvalue = value;
      TypeTag = TypeTag.OscBlob;
    }
  }

  public Atom(TypeTag type)
  {
    TypeTag = type;
    int32value = 0;
    float32value = 0;
    objvalue = null;
  }

  public Atom(int value)
  {
    float32value = 0;
    objvalue = null;
    int32value = value;
    TypeTag = TypeTag.OscInt32;
  }

  public Atom(long value)
  {
    unchecked
    {
      float32value = 0;
      objvalue = null;
      int32value = (int)value;
      TypeTag = TypeTag.OscInt32;
    }
  }

  public Atom(float value)
  {
    int32value = 0;
    objvalue = null;
    float32value = value;
    TypeTag = TypeTag.OscFloat32;
  }

  public Atom(double value)
  {
    unchecked
    {
      int32value = 0;
      objvalue = null;
      float32value = (float)value;
      TypeTag = TypeTag.OscFloat32;
    }
  }

  public Atom(string value)
  {
    int32value = 0;
    float32value = 0;
    objvalue = value;
    TypeTag = TypeTag.OscString;
  }

  public Atom(byte[] value)
  {
    int32value = 0;
    float32value = 0;
    objvalue = value;
    TypeTag = TypeTag.OscBlob;
  }


  // Methods
  public override string ToString()
  {
    switch (TypeTag)
    {
      case TypeTag.OscInt32:
        return Int32Value.ToString();
      case TypeTag.OscFloat32:
        return Float32Value.ToString(CultureInfo.InvariantCulture);
      case TypeTag.OscString:
        return StringValue ?? "<nil>";
      case TypeTag.OscBlob:
        return BlobValue != null ? BitConverter.ToString(BlobValue) : "<nil>";
      case TypeTag.OscTrue:
        return "true";
      case TypeTag.OscFalse:
        return "false";
      case TypeTag.OscNil:
        return "<nil>";
      default:
        return "unknown";
    }
  }

  public override bool Equals(object? obj)
  {
    return obj is Atom atom && Equals(atom);
  }

  public bool Equals(Atom rhs)
  {
    return AtomEqualityComparer.DefaultInstance.Equals(this, rhs);
  }

  public override int GetHashCode()
  {
    return AtomEqualityComparer.DefaultInstance.GetHashCode(this);
  }


  // TypeTag cast 
  public static implicit operator Atom(TypeTag type)
  {
    return new Atom(type);
  }

  public static implicit operator TypeTag(Atom atom)
  {
    return atom.TypeTag;
  }

  // Int32 cast
  public static implicit operator Atom(int value)
  {
    return new Atom(value);
  }

  public static implicit operator Atom(long value)
  {
    return new Atom(value);
  }

  public static explicit operator int(Atom atom)
  {
    if (atom.TypeTag != TypeTag.OscInt32) throw new InvalidCastException();
    return atom.int32value;
  }

  // Float32 cast
  public static implicit operator Atom(float value)
  {
    return new Atom(value);
  }

  public static implicit operator Atom(double value)
  {
    return new Atom(value);
  }

  public static explicit operator float(Atom atom)
  {
    if (atom.TypeTag != TypeTag.OscFloat32) throw new InvalidCastException();
    return atom.float32value;
  }

  // String cast
  public static implicit operator Atom(string value)
  {
    return new Atom(value);
  }

  public static explicit operator string?(Atom atom)
  {
    if (atom.TypeTag != TypeTag.OscString) throw new InvalidCastException();
    return atom.objvalue as string;
  }

  // Blob/binary cast
  public static implicit operator Atom(byte[] value)
  {
    return new Atom(value);
  }

  public static explicit operator byte[]?(Atom atom)
  {
    if (atom.TypeTag != TypeTag.OscBlob) throw new InvalidCastException();
    return atom.objvalue as byte[];
  }
}