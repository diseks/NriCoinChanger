using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NRI.CoinChanger;
using NRI.CoinChangerMDB;
using System.Windows.Forms;


namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (CoinChanger c = new CoinChanger())
                {
                    c.CoinAccepted += new EventHandler<CoinEventArgs>(c_CoinAccepted);
                    c.CoinChangerError += new EventHandler<StatusEventArgs>(c_CoinChangerError);
                    c.CoinChangerStatus += new EventHandler<StatusEventArgs>(c_CoinChangerStatus);
                    c.Open();

                    if (c.IsOpen)
                    {
                        c.Start();
                        c.EnableCoinAccepting();

                        System.Threading.Thread.Sleep(4000);
                        if (c.LastCoinChangerStatus.Code == 1  )
                        {
                            Console.WriteLine("Статус монетника: " + c.LastCoinChangerStatus.Message);

                            Console.WriteLine("Сумма доступной сдачи: " + (c.Coins.Sum(r => r.Sum) / 100).ToString() + " руб.");

                            Console.WriteLine("Минимальная монета для сдачи: " + (c.Coins.MinPayout/100).ToString() + " руб.");
                            
                            Console.WriteLine("Максимальная монета для сдачи: " + (c.Coins.MaxPayout/100).ToString() + " руб.");
                            

                            Console.ReadKey();
                            Console.WriteLine("Сумма доступной сдачи: " + (c.Coins.Sum(r => r.Sum) / 100).ToString() + " руб.");

                            Console.ReadKey();

                            if (c.TryPayout(3000))
                            {
                                Console.WriteLine("Выдана сдача " + (500 / 100).ToString() + " руб.");
                            }
                        }
                        else
                        {
                            throw new ArgumentException(c.LastCoinChangerStatus.Message);
                        }

                        c.DisableCoinAccepting();
                        c.Stop();
                    }
                }
            
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        static void c_CoinChangerStatus(object sender, StatusEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void c_CoinChangerError(object sender, StatusEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void c_CoinAccepted(object sender, CoinEventArgs e)
        {
            Console.WriteLine("Получено " + (e.Nominal / 100).ToString() + " руб.");
        }
    }
}
