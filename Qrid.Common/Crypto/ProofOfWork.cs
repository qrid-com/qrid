using Org.BouncyCastle.Bcpg;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

public sealed class ProofOfWork
{

    // TODO - change to SHA3. Althoug Bitcoin ASICs do SHA256(SHA256(data)), there is a higher chance to encounter fast SHA256 calc, maybe FPGA and whatever. Well, would not be too difficult to have SHA3 FPGA or GPU design I guess
    // IOS - System.Security.Cryptography.SHA3 since iOS 13 (2019) uses native implementation
    // Android - Android 12.0 (API Level 31) and later (officially released on October 4, 2021)

    // if (SHA3_256.IsSupported) 
    // {
    //     // Use native SHA3
    // }
    // else use BouncyCastle's SHA3 (slower than native) <<< may need this for older Android


    /// <summary>
    /// Computes a PoW hash by incrementing an embedded nonce inside the payload
    /// until the SHA256 hash has at least <paramref name="difficulty"/> leading zero bits.
    /// </summary>
    public static void ComputeHash(
            byte[] payload,
            int nonceOffset,
            int nonceSize,
            int difficulty,
            out byte[] hash)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));
        if (nonceOffset < 0 || nonceOffset + nonceSize > payload.Length)
            throw new ArgumentOutOfRangeException(nameof(nonceOffset));
        if (difficulty < 0 || difficulty > 256)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        using var sha256 = SHA256.Create();

        ulong nonce = 0;

        while (true)
        {
            nonce++;

            WriteNonce(payload, nonceOffset, nonce);

            hash = sha256.ComputeHash(payload);

            if (HasLeadingZeroBits(hash, difficulty))
                return;
        }
    }

    /// <summary>
    /// Verifies PoW for a payload that already contains the nonce.
    /// </summary>
    public static bool VerifyHash(
        byte[] payload,
        int difficulty)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));
        if (difficulty < 0 || difficulty > 256)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        using var sha256 = SHA256.Create();

        var hash = sha256.ComputeHash(payload);
        return HasLeadingZeroBits(hash, difficulty);
    }

    /// <summary>
    /// Estimates the average time required to find a valid PoW hash
    /// with the given difficulty on this device.
    /// </summary>
    public static TimeSpan EstimateHashComputationTime(int difficulty)
    {
        if (difficulty < 0 || difficulty > 256)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        // Small synthetic payload with embedded nonce
        byte[] payload = new byte[64];
        int nonceOffset = 0;

        using var sha256 = SHA256.Create();
        var sw = Stopwatch.StartNew();

        ulong nonce = 0;
        long hashCount = 0;

        // Benchmark duration (tradeoff: accuracy vs latency)
        const int benchmarkMs = 200;

        while (sw.ElapsedMilliseconds < benchmarkMs)
        {
            nonce++;
            WriteNonce(payload, nonceOffset, nonce);
            sha256.ComputeHash(payload);
            hashCount++;
        }

        sw.Stop();

        double hashesPerSecond = hashCount / sw.Elapsed.TotalSeconds;

        if (hashesPerSecond <= 0)
            return TimeSpan.MaxValue;

        // Expected attempts = 2^difficulty
        double expectedAttempts = Math.Pow(2.0, difficulty);

        double seconds = expectedAttempts / hashesPerSecond;
        return TimeSpan.FromSeconds(seconds);
    }

    private static void WriteNonce(byte[] payload, int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(
                payload.AsSpan(offset, 8),
                value);
    }

    /// <summary>
    /// Checks whether a hash has at least N leading zero bits.
    /// </summary>
    private static bool HasLeadingZeroBits(byte[] hash, int difficulty)
    {
        int fullBytes = difficulty / 8;
        int remainingBits = difficulty % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (hash[i] != 0)
                return false;
        }

        if (remainingBits == 0)
            return true;

        byte mask = (byte)(0xFF << (8 - remainingBits));
        return (hash[fullBytes] & mask) == 0;
    }
}
