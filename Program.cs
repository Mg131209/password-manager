using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

class Program
{
    static void Main()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
██████╗  █████╗ ███████╗██████╗ ███████╗██████╗ 
██╔══██╗██╔══██╗██╔════╝██╔══██╗██╔════╝██╔══██╗
██████╔╝███████║███████╗██████╔╝█████╗  ██████╔╝
██╔═══╝ ██╔══██║╚════██║██╔═══╝ ██╔══╝  ██╔══██╗
██║     ██║  ██║███████║██║     ███████╗██║  ██║
╚═╝     ╚═╝  ╚═╝╚══════╝╚═╝     ╚══════╝╚═╝  ╚═╝
");
        Console.ResetColor();
        Console.WriteLine("🔐 Welcome to the Password Manager\n");

        new PasswordManager().Run();
    }
}

class PasswordManager
{
    const int defaultLength = 13;

    string dbPath = Path.Combine(AppContext.BaseDirectory, "passwords.txt");
    string masterPasswordPath = Path.Combine(AppContext.BaseDirectory, ".masterpassword");

    List<PasswordObject> passwords;
    string? masterPassword;

    public PasswordManager()
    {
        passwords = LoadPasswords();
        masterPassword = GetMasterPassword();

        if (masterPassword != null)
            Login();
        else
            SetNewMasterPassword();
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
            Header("MAIN MENU");

            Console.WriteLine("1) Generate Password");
            Console.WriteLine("2) Save Password");
            Console.WriteLine("3) View Passwords");
            Console.WriteLine("4) Delete Password");
            Console.WriteLine("\nCtrl + C to quit");
            Console.Write("\n➡ Select option: ");

            switch (Console.ReadLine())
            {
                case "1": RenderGeneratePasswordMenu(); break;
                case "2": RenderSavePasswordMenu(); break;
                case "3": ViewPasswords(); break;
                case "4": RenderDeleteMenu(); break;
                default: Error("Invalid option"); break;
            }
        }
    }

    void RenderSavePasswordMenu()
    {
        Header("SAVE PASSWORD");
        Console.Write("🔒 Password: ");
        string pwd = Console.ReadLine() ?? "";
        Console.Write("🏷 Name: ");
        AddPassword(Console.ReadLine() ?? "Unnamed", pwd);
    }

    void RenderDeleteMenu()
    {
        Header("DELETE PASSWORD");

        if (passwords.Count == 0)
        {
            Info("No passwords saved");
            Pause();
            return;
        }

        for (int i = 0; i < passwords.Count; i++)
            Console.WriteLine($"[{i}] {passwords[i].name}");

        Console.Write("\n➡ Index: ");
        if (int.TryParse(Console.ReadLine(), out int iDel) &&
            iDel >= 0 && iDel < passwords.Count)
        {
            passwords.RemoveAt(iDel);
            SavePasswords();
            Success("Password deleted");
        }
        else
            Error("Invalid index");

        Pause();
    }

    void RenderGeneratePasswordMenu()
    {
        Header("GENERATE PASSWORD");

        Console.Write($"Length ({defaultLength}): ");
        int.TryParse(Console.ReadLine(), out int len);
        if (len <= 0) len = defaultLength;

        string pwd = GeneratePassword(len);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n🔐 {pwd}");
        Console.ResetColor();

        Console.Write("\nSave this password? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            Console.Write("🏷 Name: ");
            AddPassword(Console.ReadLine() ?? "Unnamed", pwd);
        }
        else
            Pause();
    }

    string GeneratePassword(int len)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random r = new Random();
        string p = "";

        for (int i = 0; i < len; i++)
        {
            if (i > 0 && i % 5 == 0) p += "-";
            p += chars[r.Next(chars.Length)];
        }
        return p;
    }

    void AddPassword(string name, string pwd)
    {
        passwords.Add(new PasswordObject(name, Encrypt(pwd)));
        SavePasswords();
        Success("Password saved");
        Pause();
    }

    void ViewPasswords()
    {
        Header("SAVED PASSWORDS");

        if (passwords.Count == 0)
        {
            Info("No passwords saved");
            Pause();
            return;
        }

        foreach (var p in passwords)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🔑 {p.name}");
            Console.ResetColor();
            Console.WriteLine($"   {Decrypt(p.password)}\n");
        }

        Pause();
    }

    void SavePasswords()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        File.WriteAllText(dbPath, JsonSerializer.Serialize(passwords, opts));
    }

    List<PasswordObject> LoadPasswords()
    {
        if (!File.Exists(dbPath))
            return new List<PasswordObject>();

        var opts = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        return JsonSerializer.Deserialize<List<PasswordObject>>(File.ReadAllText(dbPath), opts)
               ?? new List<PasswordObject>();
    }

    string? GetMasterPassword()
    {
        if (!File.Exists(masterPasswordPath)) return null;
        return File.ReadAllText(masterPasswordPath);
    }

    void SetNewMasterPassword()
    {
        Header("SET MASTER PASSWORD");
        Console.Write("🔑 Password: ");
        masterPassword = Console.ReadLine();

        if (string.IsNullOrEmpty(masterPassword))
            Environment.Exit(0);

        File.WriteAllText(masterPasswordPath, Hash(masterPassword));
        Success("Master password set");
        Pause();
    }

    void Login()
    {
        Header("LOGIN");
        Console.Write("🔐 Password: ");
        string input = Console.ReadLine() ?? "";

        if (Hash(input) != masterPassword)
        {
            Error("Wrong password");
            Environment.Exit(0);
        }

        masterPassword = input;
        Success("Login successful");
        Pause();
    }

    string Hash(string s)
    {
        using SHA256 sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    byte[] Key()
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(masterPassword!));
    }

    string Encrypt(string s)
    {
        using var aes = Aes.Create();
        aes.Key = Key();
        aes.GenerateIV();

        byte[] enc = aes.CreateEncryptor()
            .TransformFinalBlock(Encoding.UTF8.GetBytes(s), 0, s.Length);

        return $"{Convert.ToBase64String(aes.IV)}:{Convert.ToBase64String(enc)}";
    }

    string Decrypt(string s)
    {
        var p = s.Split(':');
        using var aes = Aes.Create();
        aes.Key = Key();
        aes.IV = Convert.FromBase64String(p[0]);

        byte[] dec = aes.CreateDecryptor()
            .TransformFinalBlock(Convert.FromBase64String(p[1]), 0, Convert.FromBase64String(p[1]).Length);

        return Encoding.UTF8.GetString(dec);
    }

    // ===== UI HELPERS =====

    void Header(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {title} ===\n");
        Console.ResetColor();
    }

    void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✔ {msg}");
        Console.ResetColor();
    }

    void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✖ {msg}");
        Console.ResetColor();
        Pause();
    }

    void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {msg}");
        Console.ResetColor();
    }

    void Pause()
    {
        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}

class PasswordObject
{
    public string name { get; set; } = "";
    public string password { get; set; } = "";
    public string id { get; set; } = "";

    public PasswordObject() { }

    public PasswordObject(string n, string p)
    {
        name = n;
        password = p;
        id = Guid.NewGuid().ToString();
    }
}
