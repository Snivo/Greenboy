using System;
using System.Text.RegularExpressions;

namespace GreenboyV2
{
    public static class IntExtender
    {
        public static string ToBinary( this int val, int spacePattern = 4) => Regex.Replace(Convert.ToString(val, 2).PadLeft(32, '0'), $".{{{spacePattern}}}", "$0 ").TrimEnd(' ');
 
        public static string ToBinary(this ushort val, int spacePattern = 4) => Regex.Replace(Convert.ToString(val, 2).PadLeft(16, '0'), $".{{{spacePattern}}}", "$0 ").TrimEnd(' ');

        public static string ToBinary(this byte val, int spacePattern = 4) => Regex.Replace(Convert.ToString(val, 2).PadLeft(8, '0'), $".{{{spacePattern}}}", "$0 ").TrimEnd(' ');
        
        public static string ToBinary(this sbyte val, int spacePattern = 4) => Regex.Replace(Convert.ToString(val, 2).PadLeft(8, '0'), $".{{{spacePattern}}}", "$0 ").TrimEnd(' ');

        /// <summary>
        /// Creates an unsigned short using this byte as the high byte
        /// </summary>
        public static ushort JoinHigh(this byte val, byte low) => (ushort)((val << 8) | low);

        /// <summary>
        /// Creates an unsigned short using this byte as the low byte
        /// </summary>
        public static ushort JoinLow(this byte val, byte high) => (ushort)((high << 8) | val);

        public static byte SetBit(this byte val, int bit) => (byte)(val | (1 << bit));

        public static byte SetBit(this byte val, int bit, bool set) => (byte)(set ? (val | (1 << bit)) : (val & ~(1 << bit)));

        public static byte ResetBit(this byte val, int bit) => (byte)(val & ~(1 << bit));

        public static bool GetBit(this byte val, int bit) => (val & bit) != 0;

        public static void SetBits(this ref byte val, byte set, byte bits) => val = (byte)((val & ~bits) | (bits & set));
    }
}
