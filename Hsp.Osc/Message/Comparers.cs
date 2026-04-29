using System;
using System.Collections.Generic;

namespace Hsp.Osc;

public class AtomEqualityComparer : IEqualityComparer<Atom>
{
  public static readonly AtomEqualityComparer DefaultInstance = new();

  #region IEqualityComparer<Atom> Members

  public bool Equals(Atom x, Atom y)
  {
    if (x.TypeTag != y.TypeTag) return false;

    switch (x.TypeTag)
    {
      case TypeTag.OscInt32:
        return x.Int32Value == y.Int32Value;
      case TypeTag.OscFloat32:
        return Math.Abs(x.Float32Value - y.Float32Value) < 0.01;
      case TypeTag.OscDouble:
        return Math.Abs(x.Double64Value - y.Double64Value) < double.Epsilon * 8;
      case TypeTag.OscInt64:
        return x.Int64Value == y.Int64Value;
      case TypeTag.OscTimetag:
        return x.TimetagValue == y.TimetagValue;
      case TypeTag.OscString:
        return x.StringValue == y.StringValue;
      case TypeTag.OscSymbol:
        return x.SymbolValue == y.SymbolValue;
      case TypeTag.OscChar:
        return x.CharValue == y.CharValue;
      case TypeTag.OscRgbaColor:
        return x.RgbaValue == y.RgbaValue;
      case TypeTag.OscBlob:
        return BlobEqualityComparer.DefaultInstance.Equals(x.BlobValue, y.BlobValue);
      default:
        return false;
    }
  }

  public int GetHashCode(Atom obj)
  {
    switch (obj.TypeTag)
    {
      case TypeTag.OscInt32:
        return obj.Int32Value.GetHashCode();
      case TypeTag.OscFloat32:
        return obj.Float32Value.GetHashCode();
      case TypeTag.OscDouble:
        return obj.Double64Value.GetHashCode();
      case TypeTag.OscInt64:
        return obj.Int64Value.GetHashCode();
      case TypeTag.OscTimetag:
        return obj.TimetagValue.GetHashCode();
      case TypeTag.OscString:
        return obj.StringValue == null ? 0 : obj.StringValue.GetHashCode();
      case TypeTag.OscSymbol:
        return obj.SymbolValue == null ? 0 : obj.SymbolValue.GetHashCode();
      case TypeTag.OscChar:
        return obj.CharValue.GetHashCode();
      case TypeTag.OscRgbaColor:
        return obj.RgbaValue.GetHashCode();
      case TypeTag.OscBlob:
        return obj.BlobValue == null ? 0 : obj.BlobValue.GetHashCode();
      default:
        return base.GetHashCode();
    }
  }

  #endregion
}