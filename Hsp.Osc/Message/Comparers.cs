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
      case TypeTag.OscString:
        return x.StringValue == y.StringValue;
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
      case TypeTag.OscString:
        return obj.StringValue == null ? 0 : obj.StringValue.GetHashCode();
      case TypeTag.OscBlob:
        return obj.BlobValue == null ? 0 : obj.BlobValue.GetHashCode();
      default:
        return base.GetHashCode();
    }
  }

  #endregion
}