using System.Diagnostics;
using System.Net;
using OscDotNet.Lib;

namespace OscSender;

public class ClipMatrix
{
  public Track[] Tracks { get; } = Enumerable.Range(0, 8).Select(i => new Track(i)).ToArray();

  private readonly OscUdpServer _receiver = new(IPAddress.Any, 9000);
  private readonly OscUdpClient _sender = new(IPAddress.Broadcast, 8000);
  private readonly SemaphoreSlim _updateLock = new(1, 1);


  public ClipMatrix()
  {
    _receiver.RegisterHandlers(this);
    //_receiver.MessageReceived += (_, args) => { Console.WriteLine(args.Message.Address); };
  }

  public async Task Connect()
  {
    _receiver.BeginListen();
    await _sender.ConnectAsync();
  }

  public async Task Refresh()
  {
    var msg = new Message("/refresh");
    await _sender.SendMessageAsync(msg);
  }

  [OscMessageHandler("/update")]
  private async Task UpdateComplete(MessageHandlerContext ctx)
  {
    var tag = ctx.Message.Atoms[0].Int32Value;

    if (tag == 0) Debug.WriteLine("Update completed");
    if (tag == 1) Debug.WriteLine("Update started");
    if (tag != 0) return;

    await _updateLock.WaitAsync();
    try
    {
      Console.Clear();
      for (var i = 0; i < Tracks.Length; i++)
      {
        var track = Tracks[i];
        Console.SetCursorPosition(i * 5, 0);
        if (track.IsArmed)
          Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(track.Name.Length > 4 ? track.Name[..4] : track.Name);
        Console.ResetColor();
        for (var j = 0; j < track.Clips.Length; j++)
        {
          var clip = track.Clips[j];
          Console.SetCursorPosition(i * 5, j + 1);
          Console.WriteLine(clip.ToString());
        }
      }
    }
    finally
    {
      _updateLock.Release();
    }
  }

  [OscMessageHandler("/track/(?<no>[0-9]+)/(?<prop>[a-z]+)$")]
  private async Task TrackUpdateHandler(MessageHandlerContext ctx)
  {
    var trackIndex = int.Parse(ctx.Match.Groups["no"].Value);
    var track = Tracks[trackIndex - 1];
    if (ctx.Match.Groups["prop"].Value == "exists")
    {
      if (ctx.Message.Atoms[0].Int32Value == 0)
        track.Clear();
    }

    if (ctx.Match.Groups["prop"].Value == "recarm")
      track.IsArmed = ctx.Message.Atoms[0].Int32Value == 1;
    if (ctx.Match.Groups["prop"].Value == "name")
    {
      track.Name = ctx.Message.Atoms[0].StringValue;
      Debug.WriteLine($"Got name: {trackIndex} {track.Name}");
    }

    if (ctx.Match.Groups["prop"].Value == "selected")
      track.IsSelected = ctx.Message.Atoms[0].Int32Value == 1;

    await Task.CompletedTask;
  }

  [OscMessageHandler("/track/(?<no>[0-9]+)/clip/(?<y>[0-9]+)/")]
  private async Task ClickUpdateHandler(MessageHandlerContext ctx)
  {
    var trackIndex = int.Parse(ctx.Match.Groups["no"].Value);
    var clipIndex = int.Parse(ctx.Match.Groups["y"].Value);
    var lastPart = ctx.Message.Address.Split('/').Last();
    var track = Tracks[trackIndex - 1].Clips[clipIndex - 1];
    if (lastPart == "isSelected")
      track.IsSelected = ctx.Message.Atoms[0].Int32Value == 1;
    if (lastPart == "hasContent")
      track.HasContent = ctx.Message.Atoms[0].Int32Value == 1;

    await Task.CompletedTask;
  }

  public async Task RecordNextFreeSlot(MessageHandlerContext ctx)
  {
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    if (track == null) return;
    var clip = track.Clips.FirstOrDefault(c => !c.HasContent);
    if (clip == null) return;
    var msg = new Message($"/track/{track.Index + 1}/clip/{clip.Index + 1}/record");
    await _sender.SendMessageAsync(msg);
  }

  public async Task SelectNextTrack()
  {
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    var idx = track == null
      ? 0
      : track.Index + 1;
    await SelectTrack(idx);
  }

  public async Task SelectPreviousTrack()
  {
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    var idx = track == null
      ? 0
      : track.Index - 1;
    await SelectTrack(idx);
  }

  private async Task SelectTrack(int idx)
  {
    idx = Math.Clamp(idx, 0, 7);
    var msg = new Message($"/track/{idx + 1}/select").PushAtom(1);
    await _sender.SendMessageAsync(msg);
  }

  public async Task SelectPreviousClip()
  {
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    var clip = track?.Clips.FirstOrDefault(c => c.IsSelected);

    var newIdx = clip == null
      ? 0
      : clip.Index - 1;
    await SelectClip(newIdx);
  }

  public async Task SelectNextClip()
  {
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    var clip = track?.Clips.FirstOrDefault(c => c.IsSelected);

    var newIdx = clip == null
      ? 0
      : clip.Index + 1;
    await SelectClip(newIdx);
  }

  private async Task SelectClip(int idx)
  {
    idx = Math.Clamp(idx, 0, 7);
    var track = Tracks.FirstOrDefault(t => t.IsSelected);
    if (track == null) return;
    var msg = new Message($"/track/{track.Index + 1}/clip/{idx + 1}/select").PushAtom(1);
    await _sender.SendMessageAsync(msg);
  }
}