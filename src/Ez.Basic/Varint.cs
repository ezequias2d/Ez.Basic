using System;

namespace Ez.Basic
{
    internal static class Varint
    {
        public static int EncodedSizeOf(int value)
        {
            const uint s7 = 1 << 7;
            const uint s14 = 1 << 14;
            const uint s21 = 1 << 21;
            const uint s28 = 1 << 28;
            var n = ZigZagEncoding(value);

            if (n >= 0 && n < s7)
                return 1;
            else if(n >= s7 && n < s14)
                return 2;
            else if(n >= s14 && n < s21)
                return 3;
            else if(n >= s21 && n < s28)
                return 4;
            return 5;
        }

        public static int Encode(int value, Span<byte> dst)
        {
            var n = ZigZagEncoding(value);
            int i = 0;
            do
            {
                byte coded = (byte)(n & 0x7F);
                n >>= 7;
                if (n > 0)
                    coded |= 0x80;

                dst[i++] = coded;
            } while (n != 0);
            dst = dst.Slice(0, i);
            return i;
        }

        public static int Decode(ReadOnlySpan<byte> src, out int value)
        {
            uint result = 0;

            bool moreBytes;
            int bits = 0;
            int count = 0;
            do
            {
                uint readed = src[count];
                result |= readed << bits;
                moreBytes = (readed & 0x80) != 0;
                bits += 7;
                count++;
            } while (moreBytes);

            value = ZigZagDecoding(result);
            return count;
        }

        private static uint ZigZagEncoding(int value)
        {
            return (uint)((value >> 31) ^ (value << 1));
        }

        private static int ZigZagDecoding(uint n)
        {
            return (int)((n >> 1) ^ (-(n & 1)));
        }
    }
}
