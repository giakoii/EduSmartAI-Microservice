using System.Security.Cryptography;
using System.Text;
using Shared.Application.Interfaces;
using Shared.Application.Interfaces.Commons;
using Shared.Common.Settings;
using Shared.Common.Utils.Const;

namespace Shared.Infrastructure.Logics;

/// <summary>
/// Shared.Common logic
/// </summary>
public class CommonLogic : ICommonLogic
{
    /// <summary>
    /// Encrypt the text
    /// </summary>
    /// <param name="beforeEncrypt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string EncryptText(string beforeEncrypt)
    {
        // Check for null or empty
        ArgumentException.ThrowIfNullOrEmpty(beforeEncrypt);
        
        // Get the key and IV from configuration
        EnvLoader.Load();
        
        var key = Environment.GetEnvironmentVariable(ConstEnv.EncryptionKey);
        var iv = Environment.GetEnvironmentVariable(ConstEnv.EncryptionIv);
        
        // Check for null
        if (key == null)
        {
            throw new ArgumentException();
        }
        // Encrypt the text
        using Aes aes = Aes.Create();
        
        // Set the key and IV
        aes.Key = Encoding.UTF8.GetBytes(key);
        if (iv != null) aes.IV = Encoding.UTF8.GetBytes(iv);

        // Encrypt
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        
        // Write the text to be encrypted
        sw.Write(beforeEncrypt);
        
        // Flush and close the stream
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypt the text
    /// </summary>
    /// <param name="beforeDecrypt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string DecryptText(string beforeDecrypt)
    {
        // Check for null or empty
        ArgumentException.ThrowIfNullOrEmpty(beforeDecrypt);
        
        EnvLoader.Load();
        
        var key = Environment.GetEnvironmentVariable(ConstEnv.EncryptionKey);
        var iv = Environment.GetEnvironmentVariable(ConstEnv.EncryptionIv);
        
        // Check for null
        if (key == null)
        {
            throw new ArgumentException();
        }
        // Decrypt the text
        using Aes aes = Aes.Create();
        
        // Set the key and IV
        aes.Key = Encoding.UTF8.GetBytes(key);
        if (iv != null) aes.IV = Encoding.UTF8.GetBytes(iv);

        // Decrypt
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(beforeDecrypt));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
    
    /// <summary>
    /// Generates a random password that meets complexity requirements
    /// </summary>
    /// <param name="length">Password length (minimum 8)</param>
    /// <returns>A secure random password</returns>
    public string GenerateRandomPassword(int length = 12)
    {
        // Ensure minimum length of 8
        length = Math.Max(8, length);
        
        // Define character sets
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        
        // Create a random number generator
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        
        // Generate one character from each required set
        var password = new char[length];
        password[0] = GetRandomChar(lowerChars, rng, bytes);
        password[1] = GetRandomChar(upperChars, rng, bytes);
        password[2] = GetRandomChar(numberChars, rng, bytes);
        password[3] = GetRandomChar(specialChars, rng, bytes);
        
        // Combine all character sets for remaining positions
        string allChars = lowerChars + upperChars + numberChars + specialChars;
        
        // Fill remaining positions with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = GetRandomChar(allChars, rng, bytes);
        }
        
        // Shuffle the password to avoid predictable patterns
        ShuffleArray(password, rng, bytes);
        
        return new string(password);
    }
    
    /// <summary>
    /// Gets a random character from the provided character set
    /// </summary>
    private static char GetRandomChar(string charSet, RandomNumberGenerator rng, byte[] bytes)
    {
        rng.GetBytes(bytes);
        uint num = BitConverter.ToUInt32(bytes, 0);
        return charSet[(int)(num % charSet.Length)];
    }
    
    /// <summary>
    /// Fisher-Yates shuffle algorithm to randomize character positions
    /// </summary>
    private static void ShuffleArray(char[] array, RandomNumberGenerator rng, byte[] bytes)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            rng.GetBytes(bytes);
            uint num = BitConverter.ToUInt32(bytes, 0);
            int j = (int)(num % (i + 1));
            
            // Swap elements
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
    
    /// <summary>
    /// Generates a secure random 6-digit OTP (One-Time Password)
    /// </summary>
    /// <returns>A 6-digit numeric OTP code as string</returns>
    public string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
    
        // Convert bytes to a positive integer and take modulo to get a 6-digit number
        int value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
    
        // Format as a 6-digit string with leading zeros if needed
        return value.ToString("D6");
    }
    
}