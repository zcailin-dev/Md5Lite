using System.Security.Cryptography;
using System.Text;

namespace Md5Lite;

internal static class Program
{
    private static int _failed;

    private static void Main()
    {
        Console.WriteLine("== 标准测试向量 ==");
        Check("", "d41d8cd98f00b204e9800998ecf8427e");
        Check("a", "0cc175b9c0f1b6a831c399e269772661");
        Check("abc", "900150983cd24fb0d6963f7d28e17f72");
        Check("message digest", "f96b697d7cb7938d525a2f31aaf161d0");
        Check("abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b");
        Check("12345678901234567890123456789012345678901234567890123456789012345678901234567890",
              "57edf4a22be3c955ac49da2e2107b67a");

        Console.WriteLine();
        Console.WriteLine("== 和系统实现对比（随机字符串 × 20） ==");
        var random = new Random(42);
        for (var i = 0; i < 20; i++)
        {
            var len = random.Next(1, 200);
            var bytes = new byte[len];
            random.NextBytes(bytes);

            var mine = Md5.HashToHex(bytes);
            var official = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

            if (mine != official)
            {
                _failed++;
                Console.WriteLine($"  ✗ 不一致（{len} 字节）mine={mine} official={official}");
            }
        }
        Console.WriteLine("  完成");

        Console.WriteLine();
        Console.WriteLine("== 中文 / 文件字节 ==");
        Console.WriteLine($"  \"中国\"        -> {Md5.HashToHex("中国")}");
        Console.WriteLine($"  \"中国\"（官方） -> {Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("中国"))).ToLowerInvariant()}");

        Console.WriteLine();
        Console.WriteLine(_failed == 0 ? "全部通过 ✅" : $"有 {_failed} 项失败 ❌");
        Environment.ExitCode = _failed == 0 ? 0 : 1;
    }

    private static void Check(string input, string expected)
    {
        var actual = Md5.HashToHex(input);
        var ok = actual == expected;
        if (!ok) _failed++;
        Console.WriteLine($"  {(ok ? "✓" : "✗")} \"{Escape(input)}\" -> {actual}");
    }

    private static string Escape(string s)
        => s.Length <= 30 ? s : s[..30] + "…";
}
