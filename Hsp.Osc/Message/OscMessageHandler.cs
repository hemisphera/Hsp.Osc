using System;

namespace Hsp.Osc;

[AttributeUsage(AttributeTargets.Method)]
public class OscMessageHandlerAttribute : Attribute
{
  public string Pattern { get; }


  public OscMessageHandlerAttribute(string pattern)
  {
    Pattern = pattern;
  }
}