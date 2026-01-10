using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Serialization;
using System.Text;
using System.Security.Cryptography;
using System.Dynamic;

class Program
{
    static void Main()
    {
        Console.Clear();
        Console.WriteLine(@"
██████╗  █████╗ ███████╗██████╗ ███████╗██████╗ 
██╔══██╗██╔══██╗██╔════╝██╔══██╗██╔════╝██╔══██╗
██████╔╝███████║███████╗██████╔╝█████╗  ██████╔╝
██╔═══╝ ██╔══██║╚════██║██╔═══╝ ██╔══╝  ██╔══██╗
██║     ██║  ██║███████║██║     ███████╗██║  ██║
╚═╝     ╚═╝  ╚═╝╚══════╝╚═╝     ╚══════╝╚═╝  ╚═╝
        ");
        Console.WriteLine("Welcome to the Password Manager!\n");

        PasswordManager manager = new PasswordManager();
        manager.Run();
    }
}

class PasswordManager
{
    const int defaultLength = 13;
    const string dbPath = "/home/mio/passwords.txt";
    const string masterPasswordPath = ".masterpasword";

    private List<PasswordObject> passwords;
    private string? masterPassword;

    public PasswordManager()
    {
        passwords = LoadPasswords();
        masterPassword = getMasterPasword();

        if (masterPassword != null)
        {
            logIn();
        }
        else
        {
            setNewMasterPasword();
        }
    }

    public void Run()
    {
        RenderMainMenu();
    }

    void RenderMainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Main Menu ===");
            Console.WriteLine("(1) Generate Password");
            Console.WriteLine("(2) Save Password");
            Console.WriteLine("(3) View Passwords");
            Console.WriteLine("(4) Delete a Password");
            Console.WriteLine("Press Ctrl + C to quit");
            Console.Write("\nSelect an option: ");

            string? input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    RenderGeneratePasswordMenu();
                    break;
                case "2":
                    RenderSavePasswordMenu();
                    break;
                case "3":
                    ViewPasswords();
                    break;
                case "4":
                    RenderDeleteMenu();
                    break;
                default:
                    Console.WriteLine("❌ Please enter a valid selection.");
                    Pause();
                    break;
            }
        }
    }

    void RenderSavePasswordMenu()
    {
        Console.Write("\nEnter your password to save: ");
        string password = Console.ReadLine() ?? "";

        Console.Write("Give this password a name: ");
        string name = Console.ReadLine() ?? "";

        AddPassword(name, password);
    }

    void RenderDeleteMenu()
    {
        if (passwords.Count == 0)
        {
            Console.WriteLine("\n⚠️ No passwords to delete.");
            Pause();
            return;
        }

        Console.WriteLine("\nSelect Password to delete:");
        for (int index = 0; index < passwords.Count; index++)
        {
            string decrypted = DecryptPassword(passwords[index].password);
            Console.WriteLine($"({index}) {passwords[index].name} : {decrypted}");
        }

        int deleteIndex;
        while (true)
        {
            Console.Write("\nEnter the number of the password to delete: ");
            string input = Console.ReadLine() ?? "";

            if (int.TryParse(input, out deleteIndex) && deleteIndex >= 0 && deleteIndex < passwords.Count)
                break;

            Console.WriteLine("❌ Invalid number, try again.");
        }

        passwords.RemoveAt(deleteIndex);
        SavePasswords();
        Console.WriteLine("✅ Password deleted successfully!");
        Pause();
    }

    void RenderGeneratePasswordMenu()
    {
        Console.Write($"\nHow long should the password be? (default: {defaultLength}): ");
        int length = defaultLength;
        string? input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
            int.TryParse(input, out length);

        string password = GeneratePassword(length);
        Console.WriteLine($"\nGenerated password: {password}");

        Console.Write("\nSave this password? (y/n): ");
        string? choice = Console.ReadLine();

        if (choice?.ToLower() == "y")
        {
            Console.Write("Enter a name for this password: ");
            string name = Console.ReadLine() ?? "Unnamed";
            AddPassword(name, password);
        }
    }

    string GeneratePassword(int length)
    {
        Random rnd = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string password = "";

        for (int i = 0; i < length; i++)
        {
            if (i > 0 && i % 5 == 0)
                password += "-";

            password += chars[rnd.Next(chars.Length)];
        }

        return password;
    }

    void AddPassword(string name, string password)
    {
        string encrypted = EncryptPassword(password);
        passwords.Add(new PasswordObject(name, encrypted));
        SavePasswords();
        Console.WriteLine("✅ Password saved (encrypted).");
        Pause();
    }

    void ViewPasswords()
    {
        if (passwords.Count == 0)
        {
            Console.WriteLine("\n⚠️ No passwords saved.");
            Pause();
            return;
        }

        Console.WriteLine("\nSaved Passwords:");
        foreach (var p in passwords)
        {
            string decrypted = DecryptPassword(p.password);
            Console.WriteLine($"🔑 {p.name} : {decrypted}");
        }
        Pause();
    }

    void SavePasswords()
    {
        string json = JsonSerializer.Serialize(passwords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dbPath, json);
    }

    List<PasswordObject> LoadPasswords()
    {
        if (!File.Exists(dbPath))
            return new List<PasswordObject>();

        string json = File.ReadAllText(dbPath);
        return JsonSerializer.Deserialize<List<PasswordObject>>(json) ?? new List<PasswordObject>();
    }

    string? getMasterPasword()
    {
        if (!File.Exists(masterPasswordPath))
            return null;

        return File.ReadAllText(masterPasswordPath);
    }

    void setNewMasterPasword()
    {
        Console.Write("\n🔑 Enter a master password to protect your saved passwords: ");
        masterPassword = Console.ReadLine();
        if (string.IsNullOrEmpty(masterPassword))
        {
            Console.WriteLine("❌ Master password cannot be empty. Exiting...");
            Environment.Exit(0);
        }

        File.WriteAllText(masterPasswordPath, HashString(masterPassword));
        Console.WriteLine("✅ Master password set successfully!");
        Pause();
    }

    void logIn()
    {
        Console.Write("\n🔐 Enter your master password: ");
        string password = Console.ReadLine();

        if (password != null && masterPassword == HashString(password))
        {
            masterPassword = password; // store plaintext in memory
            Console.WriteLine("✅ Login successful!");
            Pause();
        }
        else
        {
            Console.WriteLine("❌ Wrong password. Exiting...");
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
        if (masterPassword == null)
            throw new InvalidOperationException("Master password is not set.");

        byte[] key = GetAesKey(masterPassword);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        byte[] encryptedBytes = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

        string combined = Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(encryptedBytes);
        return combined;
    }

    string DecryptPassword(string encryptedText)
    {
        if (masterPassword == null)
            throw new InvalidOperationException("Master password is not set.");

        byte[] key = GetAesKey(masterPassword);
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
    public string name { get; set; }
    public string password { get; set; }
    public string id { get; set; }

    public PasswordObject(string name, string password)
    {
        this.name = name;
        this.password = password;
        this.id = Guid.NewGuid().ToString();
    }
}
