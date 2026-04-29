namespace Hsp.Osc;

/// <summary>
///   An Open Sound Control type tag value.
///   See http://opensoundcontrol.org/spec-1_0
/// </summary>
public enum TypeTag : byte
{
  Unknown = 0,
  OscInt32 = (byte)'i',      // int32
  OscFloat32 = (byte)'f',    // float32
  OscString = (byte)'s',     // OSC-string
  OscBlob = (byte)'b',       // OSC-blob
  OscInt64 = (byte)'h',      // 64-bit big-endian two's complement integer
  OscTimetag = (byte)'t',    // OSC-timetag
  OscDouble = (byte)'d',     // 64-bit ("double") IEEE 754 floating point number
  OscSymbol = (byte)'S',     // Alternate type represented as an OSC-string
  OscChar = (byte)'c',       // An ASCII character, sent as 32 bits
  OscRgbaColor = (byte)'r',  // 32-bit RGBA color
  OscTrue = (byte)'T',       // True. No bytes are allocated in the argument data.
  OscFalse = (byte)'F',      // False. No bytes are allocated in the argument data.
  OscNil = (byte)'N',        // Nil. No bytes are allocated in the argument data.
  OscInfinitum = (byte)'I',  // Infinitum. No bytes are allocated in the argument data.
  OscArrayBegin = (byte)'[', // Indicates the beginning of an array.
  OscArrayEnd = (byte)']',   // Indicates the end of an array.
  // OscMidi = (byte)'m',    // 4-byte MIDI message (not yet implemented)
}