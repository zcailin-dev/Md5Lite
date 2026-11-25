# Md5Lite

一个从零手写的 MD5 实现（RFC 1321），只用 `System.Numerics` 和 `System.Buffers.Binary`，**不依赖 `System.Security.Cryptography`**。

写它是为了把 MD5 的四个轮函数、填充规则和小端序搞清楚——不是为了在生产环境里用 MD5。

## 运行

```bash
dotnet run
```

输出会先跑 RFC 1321 的 6 个标准测试向量，再用 20 组随机字节和 .NET 自带的 `MD5.HashData` 逐一比对。

## 用法

```csharp
using Md5Lite;

string hex = Md5.HashToHex("abc");
// 900150983cd24fb0d6963f7d28e17f72

byte[] digest = Md5.HashData(File.ReadAllBytes("a.bin"));
```

## 标准测试向量（RFC 1321 附录 A.5）

| 输入 | MD5 |
|---|---|
| `""` | `d41d8cd98f00b204e9800998ecf8427e` |
| `"a"` | `0cc175b9c0f1b6a831c399e269772661` |
| `"abc"` | `900150983cd24fb0d6963f7d28e17f72` |
| `"message digest"` | `f96b697d7cb7938d525a2f31aaf161d0` |
| `"abcdefghijklmnopqrstuvwxyz"` | `c3fcd3d76192e4007dfb496cca67e13b` |
| 80 位数字串 | `57edf4a22be3c955ac49da2e2107b67a` |

全部通过。

## 实现要点

**1. 填充**

```
消息 + 0x80 + 若干个 0x00 + 8 字节长度（比特数，小端）
填充后长度必须是 64 字节的整数倍
```

代码里这一行是关键：

```csharp
var paddedLength = ((input.Length + 8) / 64 + 1) * 64;
```

**2. 分块处理**

每 64 字节一块，读成 16 个 `uint`（**小端序**），跑 64 次循环，最后把结果加回 A/B/C/D。

**3. 四个轮函数**

```csharp
i < 16  : F = (B & C) | (~B & D)
i < 32  : G = (D & B) | (~D & C)
i < 48  : H = B ^ C ^ D
i < 64  : I = C ^ (B | ~D)
```

**4. 循环左移**

用 `BitOperations.RotateLeft`，比自己写移位干净，也不会被符号位坑。

**5. K 表**

`K[i] = floor(abs(sin(i + 1)) * 2^32)`，代码里现场算出来，省得抄 64 个十六进制常量。

## 踩过的坑

- **`int` vs `uint`**：用 `int` 做移位会变成算术位移（符号位复制），结果全错。
- **小端序**：读 4 个字节拼 `uint`，以及最后输出，都必须是 little-endian。
- **长度字段是比特数**，不是字节数。
- **别用 `BitConverter` 拼字节**，它在不同平台大小端不一样，用 `BinaryPrimitives` 明确指定。

## 安全提醒

MD5 早已被证实存在碰撞（2004 年王小云团队），**不要用它存密码、做签名、校验安全文件**。

- 存密码：`Rfc2898DeriveBytes`（PBKDF2）或 Argon2
- 校验文件完整性：SHA-256
- 这里留着它，只是因为它的结构最经典、最适合练手

## License

MIT
