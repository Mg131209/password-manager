using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        DisplayLogo();
        PasswordManager manager = new PasswordManager();
        manager.Run();
    }

    static void DisplayLogo()
    {
        Console.Clear();
        Console.WriteLine("======================================");
        Console.WriteLine("       🔐 SIMPLE PASSWORD MANAGER 🔐    ");
        Console.WriteLine("======================================\n");
    }
}

class PasswordManager
{
    const int DefaultLength = 13;
    const string DbPath = "/home/mio/passwords.txt";
    const string MasterPasswordPath = ".masterpassword";

    private List<PasswordObject> Passwords;
    private string? MasterPassword;

    public PasswordManager()
    {
        Passwords = LoadPasswords();
        MasterPassword = GetStoredMasterPassword();

        if (MasterPassword != null)
            PromptLogin();
        else
            SetNewMasterPassword();
    }

    public void Run()
    {
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("========= MAIN MENU =========\n");
            Console.WriteLine("(1) Generate Password");
            Console.WriteLine("(2) Save Password");
            Console.WriteLine("(3) View Passwords");
            Console.WriteLine("(4) Delete a Password");
            Console.WriteLine("(Ctrl + C to quit)\n");
            Console.Write("Enter your choice: ");

            string? input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    GeneratePasswordMenu();
                    break;
                case "2":
                    SavePasswordMenu();
                    break;
                case "3":
                    DisplayPasswords();
                    break;
                case "4":
                    DeletePasswordMenu();
                    break;
                default:
                    Console.WriteLine("⚠️ Invalid selection. Please try again.");
                    Pause();
                    break;
            }
        }
    }

    void SavePasswordMenu()
    {
        Console.WriteLine("💾 Enter the password to save:");
        string password = Console.ReadLine() ?? "";

        Console.WriteLine("📛 Give this password a name:");
        string name = Console.ReadLine() ?? "Unnamed";

        AddPassword(name, password);
    }

    void GeneratePasswordMenu()
    {
        Console.WriteLine($"🛠 How long should the password be? (default: {DefaultLength})");

        int length = DefaultLength;
        string? input = Console.ReadLine();

        if (!string.IsNullOrEmpty(input))
            int.TryParse(input, out length);

        string password = GeneratePassword(length);
        Console.WriteLine($"\n✅ Generated password: {password}\n");

        Console.WriteLine("💾 Save this password? (y/n)");
        string? choice = Console.ReadLine();

        if (choice?.ToLower() == "y")
        {
            Console.WriteLine("📛 Enter a name:");
            string name = Console.ReadLine() ?? "Unnamed";
            AddPassword(name, password);
        }
    }

    void DisplayPasswords()
    {
        Console.WriteLine("========= SAVED PASSWORDS =========\n");

        if (Passwords.Count == 0)
        {
            Console.WriteLine("⚠️  No passwords saved yet.\n");
            Pause();
            return;
        }

        for (int i = 0; i < Passwords.Count; i++)
        {
            string decrypted = DecryptPassword(Passwords[i].Password);
            Console.WriteLine($"({i}) {Passwords[i].Name} : {decrypted}");
        }

        Console.WriteLine();
        Pause();
    }

    void DeletePasswordMenu()
    {
        Console.WriteLine("========= DELETE A PASSWORD =========\n");

        if (Passwords.Count == 0)
        {
            Console.WriteLine("⚠️  No passwords to delete.\n");
            Pause();
            return;
        }

        for (int i = 0; i < Passwords.Count; i++)
        {
            string decrypted = DecryptPassword(Passwords[i].Password);
            Console.WriteLine($"({i}) {Passwords[i].Name} : {decrypted}");
        }

        Console.Write("\nEnter the number of the password to delete: ");
        int deleteIndex;
        while (true)
        {
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out deleteIndex))
            {
                if (deleteIndex >= 0 && deleteIndex < Passwords.Count)
                    break;
                Console.WriteLine($"⚠️ Number must be between 0 and {Passwords.Count - 1}. Try again:");
            }
            else
            {
                Console.WriteLine("⚠️ Please enter a valid number:");
            }
        }

        Passwords.RemoveAt(deleteIndex);
        SavePasswords();
        Console.WriteLine("✅ Password deleted successfully.");
        Pause();
    }

    void AddPassword(string name, string password)
    {
        string encrypted = EncryptPassword(password);
        Passwords.Add(new PasswordObject(name, encrypted));
        SavePasswords();
        Console.WriteLine("💾 Password saved successfully (encrypted).");
        Pause();
    }

    string GeneratePassword(int length)
    {
        Random rnd = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder password = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            if (i > 0 && i % 5 == 0)
                password.Append("-");

            password.Append(chars[rnd.Next(chars.Length)]);
        }

        return password.ToString();
    }

    void SavePasswords()
    {
        string json = JsonSerializer.Serialize(Passwords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DbPath, json);
    }

    List<PasswordObject> LoadPasswords()
    {
        if (!File.Exists(DbPath))
            return new List<PasswordObject>();

        string json = File.ReadAllText(DbPath);
        return JsonSerializer.Deserialize<List<PasswordObject>>(json) ?? new List<PasswordObject>();
    }

    string? GetStoredMasterPassword()
    {
        if (!File.Exists(MasterPasswordPath))
            return null;

        return File.ReadAllText(MasterPasswordPath);
    }

    void SetNewMasterPassword()
    {
        Console.WriteLine("🔑 Enter a master password to protect your passwords:");
        MasterPassword = Console.ReadLine() ?? "";
        File.WriteAllText(MasterPasswordPath, HashString(MasterPassword));
        Console.WriteLine("✅ Master password set successfully!");
        Pause();
    }

    void PromptLogin()
    {
        Console.WriteLine("🔐 Enter your master password to unlock:");
        string password = Console.ReadLine() ?? "";

        if (MasterPassword == HashString(password))
        {
            MasterPassword = password;
            Console.WriteLine("✅ Login successful!");
            Pause();
        }
        else
        {
            Console.WriteLine("❌ Wrong master password. Exiting...");
            Environment.Exit(0);
        }
    }

    string HashString(string input)
    {
        using SHA256 sha = SHA256.Create();
        byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }

    byte[] GetAesKey(string masterPassword)
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(masterPassword));
    }

    string EncryptPassword(string plainText)
    {
        byte[] key = GetAesKey(MasterPassword!);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        byte[] encryptedBytes = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

        return Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(encryptedBytes);
    }

    string DecryptPassword(string encryptedText)
    {
        byte[] key = GetAesKey(MasterPassword!);

        string[] parts = encryptedText.Split(':');
        if (parts.Length != 2)
            throw new Exception("Invalid encrypted format");

        byte[] iv = Convert.FromBase64String(parts[0]);
        byte[] cipher = Convert.FromBase64String(parts[1]);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}

class PasswordObject
{
    public string Name { get; set; }
    public string Password { get; set; }
    public string Id { get; set; }

    public PasswordObject(string name, string password)
    {
        Name = name;
        Password = password;
        Id = Guid.NewGuid().ToString();
    }
}
