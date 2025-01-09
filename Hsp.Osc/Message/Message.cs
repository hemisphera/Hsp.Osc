using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hsp.Osc;

public sealed class Message : IEnumerable<Atom>
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


  public IEnumerator<Atom> GetEnumerator()
  {
    foreach (var atom in Atoms ?? []) yield return atom;
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}