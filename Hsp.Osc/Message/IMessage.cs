using System.Threading.Tasks;

namespace Hsp.Osc;

public interface IMessage
{
  byte[] ToBytes();
}