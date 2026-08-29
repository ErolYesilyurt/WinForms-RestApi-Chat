using System;
using EfCoreExample;
using System.Linq;
using WinFormsApp2;
using Microsoft.EntityFrameworkCore;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using (var db = new EfCoreExample.AppDbContext())
        {
            db.Database.EnsureCreated();

            Application.Run(new Form1());

        }
    }

    public static string Encoder(string password)
    {
        string yeni = "";
        string temp = "";

        for (int i = 1; i < password.Length / 2 + 1; i++)
        {
            int a = (int)password[i * 2 - 1];
            int b = (int)password[i * 2 - 2];
            string as_ = a.ToString();
            string ab = b.ToString();

            while (as_.Length < 3)
                as_ = "0" + as_;

            while (ab.Length < 3)
                ab = "0" + ab;

            temp = "" + as_[1] + as_[0] + ab[2];
            yeni += (char)int.Parse(temp);

            temp = "" + ab[1] + ab[0] + as_[2];
            yeni += (char)int.Parse(temp);
        }
        if (password.Length % 2 == 1)
            yeni += password[password.Length - 1];
        return yeni;
    }

    public static string Decoder(string encoded)
    {
        string result = "";
        int i = 0;
        for (; i + 1 < encoded.Length; i += 2)
        {
            int c1 = (int)encoded[i];
            int c2 = (int)encoded[i + 1];

            string s1 = c1.ToString("D3");
            string s2 = c2.ToString("D3");

            char a = (char)int.Parse("" + s1[1] + s1[0] + s2[2]);
            char b = (char)int.Parse("" + s2[1] + s2[0] + s1[2]);
            result += b.ToString() + a.ToString();
        }
        if (encoded.Length % 2 == 1)
            result += encoded[encoded.Length - 1];
        return result;
    }
} 