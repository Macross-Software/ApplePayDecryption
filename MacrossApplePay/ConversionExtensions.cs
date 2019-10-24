using System;
using System.Collections.Generic;
using System.Linq;

namespace Macross
{
    internal static class ConversionExtensions
    {
        public static byte[] ToByteArray(this IEnumerable<char> hexData)
        {
            int Length = hexData.Count();

            if (Length % 2 == 1)
                throw new InvalidOperationException("Hex data cannot have an odd number of digits");

            byte[] Data = new byte[Length >> 1];

            int i = 0;
            int LastCharValue = -1;

            WriteHexCharsToArray(hexData, Data, ref i, ref LastCharValue);

            return Data;
        }

        private static void WriteHexCharsToArray(IEnumerable<char> hexData, byte[] data, ref int i, ref int lastCharValue)
        {
            foreach (char Char in hexData)
            {
                if (lastCharValue < 0)
                    lastCharValue = GetByteValue(Char);
                else
                {
                    data[i++] = (byte)((lastCharValue << 4) + (GetByteValue(Char)));
                    lastCharValue = -1;
                }
            }
        }

        private static int GetByteValue(char c)
        {
            int val = c - (c < 58 ? 48 : (c < 97 ? 55 : 87));
            if (val > 15 || val < 0)
                throw new ArgumentOutOfRangeException($"Character '{c}' is not a valid Hex value.");
            return val;
        }
    }
}
