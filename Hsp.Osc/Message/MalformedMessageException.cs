using System;

namespace Hsp.Osc;

public class MalformedMessageException : InvalidOperationException
{
  public byte[] MessageData { get; }

  public string? Address { get; }


  public MalformedMessageException(string message, byte[] data, string address = null)
    : base(message)
  {
    MessageData = data;
    Address = address;
  }

  public MalformedMessageException(string message, byte[] data, Exception innerException, string? address = null)
    : base(message, innerException)
  {
    MessageData = data;
    Address = address;
  }
}