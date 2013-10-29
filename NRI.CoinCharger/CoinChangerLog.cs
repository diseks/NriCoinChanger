using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NRI.CoinCharger
{
    public sealed class CoinChangerLog
    {
        public static void WriteLog(string message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Log";

            if (message != "")
                message = string.Format("{0:dd.MM.yyyy HH:mm:ss}", DateTime.Now) + "  " + message;

            if (!File.Exists(path))
                Directory.CreateDirectory(path);

            File.AppendAllText(path + "\\NRI.CoinCharger_" + DateTime.Today.Month.ToString("D2") + DateTime.Today.Year + ".txt", message + "\n");
        }
    }
}
