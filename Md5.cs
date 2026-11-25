using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Md5Lite;

/// <summary>
/// 手写 MD5（RFC 1321），不依赖 System.Security.Cryptography。
/// 仅用于学习算法结构，不要用于生产环境的密码存储。
/// </summary>
public static class Md5
{
    // 每轮左移位数：4 轮 × 4 个，循环使用
    private static readonly int[] Shift =
    {
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
        5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20,
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
    };

    // K[i] = floor(abs(sin(i + 1)) * 2^32)，标准常量表
    private static readonly uint[] K = BuildK();

    private static uint[] BuildK()
    {
        var k = new uint[64];
        for (var i = 0; i < 64; i++)
        {
            k[i] = (uint)(long)Math.Floor(Math.Abs(Math.Sin(i + 1)) * 4294967296.0);
        }
        return k;
    }

    /// <summary>计算 MD5，返回 16 字节摘要</summary>
    public static byte[] HashData(ReadOnlySpan<byte> input)
    {
        // 初始状态 A/B/C/D
        uint a0 = 0x67452301;
        uint b0 = 0xefcdab89;
        uint c0 = 0x98badcfe;
        uint d0 = 0x10325476;

        // 填充：追加 1 个 0x80，补 0 到 56 mod 64，最后 8 字节写原长度（比特，小端）
        var paddedLength = ((input.Length + 8) / 64 + 1) * 64;
        var padded = new byte[paddedLength];
        input.CopyTo(padded);
        padded[input.Length] = 0x80;

        var bitLength = (ulong)input.Length * 8UL;
        BinaryPrimitives.WriteUInt64LittleEndian(padded.AsSpan(paddedLength - 8), bitLength);

        Span<uint> m = stackalloc uint[16];

        for (var offset = 0; offset < paddedLength; offset += 64)
        {
            for (var i = 0; i < 16; i++)
            {
                m[i] = BinaryPrimitives.ReadUInt32LittleEndian(padded.AsSpan(offset + i * 4, 4));
            }

            uint a = a0, b = b0, c = c0, d = d0;

            for (var i = 0; i < 64; i++)
            {
                uint f;
                int g;

                if (i < 16)
                {
                    f = (b & c) | (~b & d);      // F = (B & C) | (!B & D)
                    g = i;
                }
                else if (i < 32)
                {
                    f = (d & b) | (~d & c);      // G = (D & B) | (!D & C)
                    g = (5 * i + 1) % 16;
                }
                else if (i < 48)
                {
                    f = b ^ c ^ d;               // H = B ^ C ^ D
                    g = (3 * i + 5) % 16;
                }
                else
                {
                    f = c ^ (b | ~d);            // I = C ^ (B | !D)
                    g = (7 * i) % 16;
                }

                var tmp = d;
                d = c;
                c = b;
                b += BitOperations.RotateLeft(a + f + K[i] + m[g], Shift[i]);
                a = tmp;
            }

            a0 += a;
            b0 += b;
            c0 += c;
            d0 += d;
        }

        var result = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(0), a0);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), b0);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), c0);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12), d0);
        return result;
    }

    /// <summary>计算 MD5，返回小写十六进制字符串</summary>
    public static string HashToHex(ReadOnlySpan<byte> input)
        => Convert.ToHexString(HashData(input)).ToLowerInvariant();

    /// <summary>对字符串计算 MD5（UTF-8），返回小写十六进制</summary>
    public static string HashToHex(string text)
        => HashToHex(Encoding.UTF8.GetBytes(text));
}
