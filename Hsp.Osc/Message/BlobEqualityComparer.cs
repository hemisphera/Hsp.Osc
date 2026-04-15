using System.Collections.Generic;

namespace Hsp.Osc;

public class BlobEqualityComparer : IEqualityComparer<byte[]>
{
  public static readonly BlobEqualityComparer DefaultInstance = new();

  public bool Equals(byte[]? x, byte[]? y)
  {
    if (x == null) return y == null;
    if (y == null) return false;

    if (x.Length != y.Length) return false;

    for (var i = 0; i < x.Length; i++)
      if (x[i] != y[i])
        return false;

    return true;
  }

  public int GetHashCode(byte[]? obj)
  {
    return obj == null ? 0 : obj.GetHashCode();
  }
}