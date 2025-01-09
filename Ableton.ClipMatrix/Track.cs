namespace OscSender;

public class Track
{
  public Clip[] Clips { get; } = Enumerable.Range(0, 8).Select(i => new Clip(i)).ToArray();
  public string Name { get; set; } = string.Empty;
  public bool IsSelected { get; set; }
  public int Index { get; }
  public bool IsArmed { get; set; }


  public Track(int index)
  {
    Index = index;
  }

  public void Clear()
  {
    Name = string.Empty;
    IsSelected = false;
    foreach (var clip in Clips)
    {
      clip.Clear();
    }
  }
}