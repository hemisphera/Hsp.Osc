namespace OscSender;

public class Clip
{
  public bool IsPlaying { get; set; }
  public bool HasContent { get; set; }
  public bool IsSelected { get; set; }
  public int Index { get; }


  public Clip(int index)
  {
    Index = index;
  }


  public override string ToString()
  {
    var content = HasContent ? "OO" : "  ";
    return IsSelected ? $"[{content}]" : $" {content} ";
  }

  public void Clear()
  {
    IsPlaying = false;
    HasContent = false;
    IsSelected = false;
  }
}