using System.Net;
using OscDotNet.Lib;
using OscSender;

/*
var sender = new OscUdpClient(IPAddress.Broadcast, 8000);
await sender.ConnectAsync();

var receiver = new OscUdpServer(IPAddress.Any, 9000);
receiver.RegisterHandler("/track/(?<x>[0-9]+)/clip/(?<y>[0-9]+)/isSelected$", ctx =>
{
  var iss = ctx.Message.Atoms[0].Int32Value == 1;
  var track = int.Parse(ctx.Match.Groups["x"].Value);
  var clip = int.Parse(ctx.Match.Groups["y"].Value);
  Console.WriteLine($"Track {track} clip {clip} is {(iss ? "selected" : "not selected")}");
});
receiver.RegisterHandler("/track/(?<x>[0-9]+)/clip/(?<y>[0-9]+)/hasContent", ctx =>
{
  var iss = ctx.Message.Atoms[0].Int32Value == 1;
  if (!iss) return;
  var track = int.Parse(ctx.Match.Groups["x"].Value);
  var clip = int.Parse(ctx.Match.Groups["y"].Value);
  Console.WriteLine($"Track {track} clip {clip} has content");
});
receiver.BeginListen();

while (true)
{
  var command = Console.ReadLine();
  if (string.IsNullOrEmpty(command)) continue;
  if (command.Equals("exit", StringComparison.OrdinalIgnoreCase) == true) Environment.Exit(0);

  var mb = new MessageBuilder
  {
    Address = command
  };

  await sender.SendMessageAsync(mb.ToMessage());
  Console.WriteLine($"sent command '{command}'");
}
*/
var mtx = new ClipMatrix();
await mtx.Connect();
//await Task.Delay(TimeSpan.FromSeconds(1));
await mtx.Refresh();

_ = Task.Run(async () =>
{
  while (true)
  {
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Escape) Environment.Exit(0);
    if (key.Key == ConsoleKey.RightArrow) await mtx.SelectNextTrack();
    if (key.Key == ConsoleKey.LeftArrow) await mtx.SelectPreviousTrack();
    if (key.Key == ConsoleKey.UpArrow) await mtx.SelectPreviousClip();
    if (key.Key == ConsoleKey.DownArrow) await mtx.SelectNextClip();
  }
});
while (true) await Task.Delay(1000);