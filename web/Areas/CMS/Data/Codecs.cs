﻿// FROM https://rextester.com/TGN19503

using System.Text;

namespace Viper.Areas.CMS.Data
{
    public static class Codecs
    {
        static readonly byte[] UUEncMap = "`!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_"u8.ToArray();

        static readonly byte[] UUDecMap = new byte[]
        {
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
          0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
          0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
          0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
          0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
          0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
          0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
          0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        static readonly byte[] XXEncMap = "+-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8.ToArray();

        static readonly byte[] XXDecMap = new byte[]
        {
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
          0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
          0x0A, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12,
          0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A,
          0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22,
          0x23, 0x24, 0x25, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C,
          0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34,
          0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C,
          0x3D, 0x3E, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public static void UUDecode(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            long len = input.Length;
            if (len == 0)
                return;

            long didx = 0;
            int nextByte = input.ReadByte();
            while (nextByte >= 0)
            {
                // get line length (in number of encoded octets)
                int line_len = UUDecMap[nextByte];

                // ascii printable to 0-63 and 4-byte to 3-byte conversion
                long end = didx + line_len;
                byte A, B, C, D;
                if (end > 2)
                {
                    while (didx < end - 2)
                    {
                        A = UUDecMap[input.ReadByte()];
                        B = UUDecMap[input.ReadByte()];
                        C = UUDecMap[input.ReadByte()];
                        D = UUDecMap[input.ReadByte()];

                        output.WriteByte((byte)(((A << 2) & 255) | ((B >> 4) & 3)));
                        output.WriteByte((byte)(((B << 4) & 255) | ((C >> 2) & 15)));
                        output.WriteByte((byte)(((C << 6) & 255) | (D & 63)));
                        didx += 3;
                    }
                }

                if (didx < end)
                {
                    A = UUDecMap[input.ReadByte()];
                    B = UUDecMap[input.ReadByte()];
                    output.WriteByte((byte)(((A << 2) & 255) | ((B >> 4) & 3)));
                    didx++;

                    if (didx < end)
                    {
                        C = UUDecMap[input.ReadByte()];
                        output.WriteByte((byte)(((B << 4) & 255) | ((C >> 2) & 15)));
                        didx++;
                    }
                }

                // skip padding
                do
                {
                    nextByte = input.ReadByte();
                }
                while (nextByte >= 0 && nextByte != '\n' && nextByte != '\r');

                // skip end of line
                do
                {
                    nextByte = input.ReadByte();
                }
                while (nextByte >= 0 && (nextByte == '\n' || nextByte == '\r'));
            }
        }

        public static void UUEncode(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            long len = input.Length;
            if (len == 0)
                return;

            int sidx = 0;
            int line_len = 45;
            byte[] nl = Encoding.ASCII.GetBytes(Environment.NewLine);

            byte A, B, C;
            // split into lines, adding line-length and line terminator
            while (sidx + line_len < len)
            {
                // line length
                output.WriteByte(UUEncMap[line_len]);

                // 3-byte to 4-byte conversion + 0-63 to ascii printable conversion
                for (int end = sidx + line_len; sidx < end; sidx += 3)
                {
                    A = (byte)input.ReadByte();
                    B = (byte)input.ReadByte();
                    C = (byte)input.ReadByte();

                    output.WriteByte(UUEncMap[(A >> 2) & 63]);
                    output.WriteByte(UUEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                    output.WriteByte(UUEncMap[(C >> 6) & 3 | (B << 2) & 63]);
                    output.WriteByte(UUEncMap[C & 63]);
                }

                // line terminator
                for (int idx = 0; idx < nl.Length; idx++)
                    output.WriteByte(nl[idx]);
            }

            // line length
            output.WriteByte(UUEncMap[len - sidx]);

            // 3-byte to 4-byte conversion + 0-63 to ascii printable conversion
            while (sidx + 2 < len)
            {
                A = (byte)input.ReadByte();
                B = (byte)input.ReadByte();
                C = (byte)input.ReadByte();

                output.WriteByte(UUEncMap[(A >> 2) & 63]);
                output.WriteByte(UUEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                output.WriteByte(UUEncMap[(C >> 6) & 3 | (B << 2) & 63]);
                output.WriteByte(UUEncMap[C & 63]);
                sidx += 3;
            }

            if (sidx < len - 1)
            {
                A = (byte)input.ReadByte();
                B = (byte)input.ReadByte();

                output.WriteByte(UUEncMap[(A >> 2) & 63]);
                output.WriteByte(UUEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                output.WriteByte(UUEncMap[(B << 2) & 63]);
                output.WriteByte(UUEncMap[0]);
            }
            else if (sidx < len)
            {
                A = (byte)input.ReadByte();

                output.WriteByte(UUEncMap[(A >> 2) & 63]);
                output.WriteByte(UUEncMap[(A << 4) & 63]);
                output.WriteByte(UUEncMap[0]);
                output.WriteByte(UUEncMap[0]);
            }

            // line terminator
            for (int idx = 0; idx < nl.Length; idx++)
                output.WriteByte(nl[idx]);
        }

        public static void XXDecode(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            long len = input.Length;
            if (len == 0)
                return;

            long didx = 0;
            int nextByte = input.ReadByte();
            while (nextByte >= 0)
            {
                // get line length (in number of encoded octets)
                int line_len = XXDecMap[nextByte];

                // ascii printable to 0-63 and 4-byte to 3-byte conversion
                long end = didx + line_len;
                byte A, B, C, D;
                if (end > 2)
                {
                    while (didx < end - 2)
                    {
                        A = XXDecMap[input.ReadByte()];
                        B = XXDecMap[input.ReadByte()];
                        C = XXDecMap[input.ReadByte()];
                        D = XXDecMap[input.ReadByte()];

                        output.WriteByte((byte)(((A << 2) & 255) | ((B >> 4) & 3)));
                        output.WriteByte((byte)(((B << 4) & 255) | ((C >> 2) & 15)));
                        output.WriteByte((byte)(((C << 6) & 255) | (D & 63)));
                        didx += 3;
                    }
                }

                if (didx < end)
                {
                    A = XXDecMap[input.ReadByte()];
                    B = XXDecMap[input.ReadByte()];
                    output.WriteByte((byte)(((A << 2) & 255) | ((B >> 4) & 3)));
                    didx++;

                    if (didx < end)
                    {
                        C = XXDecMap[input.ReadByte()];
                        output.WriteByte((byte)(((B << 4) & 255) | ((C >> 2) & 15)));
                        didx++;
                    }
                }

                // skip padding
                do
                {
                    nextByte = input.ReadByte();
                }
                while (nextByte >= 0 && nextByte != '\n' && nextByte != '\r');

                // skip end of line
                do
                {
                    nextByte = input.ReadByte();
                }
                while (nextByte >= 0 && (nextByte == '\n' || nextByte == '\r'));
            }
        }

        public static void XXEncode(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            long len = input.Length;
            if (len == 0)
                return;

            int sidx = 0;
            int line_len = 45;
            byte[] nl = Encoding.ASCII.GetBytes(Environment.NewLine);

            byte A, B, C;
            // split into lines, adding line-length and line terminator
            while (sidx + line_len < len)
            {
                // line length
                output.WriteByte(XXEncMap[line_len]);

                // 3-byte to 4-byte conversion + 0-63 to ascii printable conversion
                for (int end = sidx + line_len; sidx < end; sidx += 3)
                {
                    A = (byte)input.ReadByte();
                    B = (byte)input.ReadByte();
                    C = (byte)input.ReadByte();

                    output.WriteByte(XXEncMap[(A >> 2) & 63]);
                    output.WriteByte(XXEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                    output.WriteByte(XXEncMap[(C >> 6) & 3 | (B << 2) & 63]);
                    output.WriteByte(XXEncMap[C & 63]);
                }

                // line terminator
                for (int idx = 0; idx < nl.Length; idx++)
                    output.WriteByte(nl[idx]);
            }

            // line length
            output.WriteByte(XXEncMap[len - sidx]);

            // 3-byte to 4-byte conversion + 0-63 to ascii printable conversion
            while (sidx + 2 < len)
            {
                A = (byte)input.ReadByte();
                B = (byte)input.ReadByte();
                C = (byte)input.ReadByte();

                output.WriteByte(XXEncMap[(A >> 2) & 63]);
                output.WriteByte(XXEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                output.WriteByte(XXEncMap[(C >> 6) & 3 | (B << 2) & 63]);
                output.WriteByte(XXEncMap[C & 63]);
                sidx += 3;
            }

            if (sidx < len - 1)
            {
                A = (byte)input.ReadByte();
                B = (byte)input.ReadByte();

                output.WriteByte(XXEncMap[(A >> 2) & 63]);
                output.WriteByte(XXEncMap[(B >> 4) & 15 | (A << 4) & 63]);
                output.WriteByte(XXEncMap[(B << 2) & 63]);
                output.WriteByte(XXEncMap[0]);
            }
            else if (sidx < len)
            {
                A = (byte)input.ReadByte();

                output.WriteByte(XXEncMap[(A >> 2) & 63]);
                output.WriteByte(XXEncMap[(A << 4) & 63]);
                output.WriteByte(XXEncMap[0]);
                output.WriteByte(XXEncMap[0]);
            }

            // line terminator
            for (int idx = 0; idx < nl.Length; idx++)
                output.WriteByte(nl[idx]);
        }
    }
}
