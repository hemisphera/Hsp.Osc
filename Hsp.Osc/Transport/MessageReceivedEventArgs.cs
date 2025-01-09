using System;

namespace Hsp.Osc;

public class MessageReceivedEventArgs : EventArgs
{
  public MessageReceivedEventArgs(Message message)
  {
    Message = message;
  }

  public Message Message { get; private set; }
}