using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StegImageUI.Extensions
{
    public static class BitArrayExtension
    {
        /// <summary>
        /// Convert an integer to a binary
        /// </summary>
        /// <param name="numeral"></param>
        /// <returns></returns>
        public static BitArray ToBinary(this int numeral)
        {
            return new BitArray(new[] { numeral });
        }

        /// <summary>
        /// Convert a binary to an integer
        /// </summary>
        /// <param name="binary"></param>
        /// <returns></returns>
        public static int ToInteger(this BitArray binary)
        {
            if (binary == null)
                throw new ArgumentNullException("binary");
            if (binary.Length > 32)
                throw new ArgumentException("must be at most 32 bits long");

            var result = new int[1];
            binary.CopyTo(result, 0);
            return result[0];
        }


        /// <summary>
        /// Prepend to a source BitArray (current) a target BitArray (before)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="before"></param>
        /// <returns></returns>
        public static BitArray Prepend(this BitArray current, BitArray before)
        {
            var bools = new bool[current.Count + before.Count];
            before.CopyTo(bools, 0);
            current.CopyTo(bools, before.Count);
            return new BitArray(bools);
        }

        /// <summary>
        /// Append to a source BitArray (current) a target BitArray (after)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public static BitArray Append(this BitArray current, BitArray after)
        {
            var bools = new bool[current.Count + after.Count];
            current.CopyTo(bools, 0);
            after.CopyTo(bools, current.Count);
            return new BitArray(bools);
        }

        public static string ToDigitString(this BitArray array)
        {
            var builder = new StringBuilder();
            foreach (var bit in array.Cast<bool>())
                builder.Append(bit ? "1" : "0");
            return builder.ToString();
        }

        public static BitArray ToBitArray(this string content)
        {
            var bools = new bool[content.Length];
            for (int i = 0; i < content.Length; i++)
                bools[i] = content[i] == '1';

            return new BitArray(bools);
        }
    }
}
