using System;
using System.Text.Json;

class Program
{
    const int defaultLength = 13;
    const string dbPath = "passwords.txt";
    PasswordObject[] passwords = getPasswords();
    static void Main()
    {
        
        getPasswords();
        renderMainMenu();
    }

    static void renderMainMenu()
    {

        while (true)
        {
        Console.WriteLine("Pleas select your action");
        Console.WriteLine("(1) Generate Password");
        Console.WriteLine("(2) Save Password");
        Console.WriteLine("(3) View Passwords");
        Console.WriteLine("Ctrl + c to quit");
      
            string? input = Console.ReadLine();

            switch (input)
            {
                case "1":
                renderGeneratePassworMenu();
                    break;
                case "2":
                renderSavePasswordMenu();
                    break;
                case "3":
                    break;
                
                default:
                    Console.WriteLine("Pleas Enter a valid selection");
                    Console.Clear();
                    break;
            }   
        }
    }
static void renderSavePasswordMenu()
    {
        Console.WriteLine("Enter your password");
        string password = Console.ReadLine();
        Console.WriteLine("Give this password a name");
        string name = Console.ReadLine();
        addPasswordToList(name, password);
    }
static void renderGeneratePassworMenu()
{
    
    Console.WriteLine($"How long would you like your password to be(default : {defaultLength}): ");
    int lenght = defaultLength;
        while (true)
        {
            string? input = Console.ReadLine();
            if(input == "")
            {
                break;
            }
            if(! int.TryParse(input, out  lenght))
            {
                Console.WriteLine("pleas enter a number");
            }
        }
        string password = GeneratePassword(lenght);
    Console.WriteLine(password);
    Console.WriteLine("Do you want to save this  to your passwords?(Y/n)");
    bool exit = false;
        while (true)
        {
            
            string?  input = Console.ReadLine();
                switch (input)
                {
                    case  "":
                        exit = true;
                        //save password
                        break;
                    case "Y":
                    exit = true;
                        //save password
                        break;
                    case "y":
                    exit = true;
                        addPasswordToList(password);
                        break;
                    case "N":
                    exit = true;
                        break;
                    case "n":
                    exit = true;
                        break;
                    
                    default:
                        Console.WriteLine("pleas Enter y(yes) or n(now)");
                        break;

                }
            if (exit)
            {
                break;
            }
        }
}
    static string GeneratePassword(int length)
    {
        Random rnd = new Random();
        string password = "";

        for (int i = 0; i < length; i++)
        {
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            if(i % 5 == 0 && i != length && i != 0)
            {
                password +="-";
            }
            else
            {   
                char charakter = chars[rnd.Next(chars.Length)]; 
                password += charakter;
            }
        }

        return password;
    }
    static void addPasswordToList(string name, string password)
    {
        PasswordObject passwordObject = new PasswordObject
        {
            name = name,
            password = password,
            id = Guid.NewGuid().ToString(),
        };

        passwords.Add(passwordObject);
        savePaswords();
    }
    static void savePaswords()
    {
        StreamWriter writer = new StreamWriter(dbPath);
        Console.WriteLine("How would you like to cal this password: ");
        string? input = Console.ReadLine();
        
        string jsonString = JsonSerializer.Serialize(passwordObject);

        if(File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
        writer.Write(jsonString);
    }

    static PasswordObject[] getPasswords()
    {
        StreamReader reader = new StreamReader(dbPath);

        string? line = reader.ReadLine();
        string jsonString = "";

        while(line != null)
        {
            jsonString += line;

            line = reader.ReadLine();
        }
        reader.Close();
    }
    
}

class PasswordObject
{
    public string name {get; set;}
    public string password {get; set;}
    public string id{get; set;}
}