using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;


namespace NewLotto
{
    class Program
    {
        static void Main(string[] args)
        {
            AppMain app = new AppMain();


            List<byte[]> aryAllPatterns = app.GetAllPatterns();
            Console.WriteLine(aryAllPatterns.Count);

            //int nSize = System.Runtime.InteropServices.Marshal.SizeOf(aryAllPatterns);
            Console.WriteLine(7 * aryAllPatterns.Count / 1024 / 1024);



            // app.LoadBalls();

            // app.AnalysisCountLevel();

            //app.AnalysisCalculator();

            for (int i = 0; i < 5; i++)
            {

                List<byte> aryResult = app.GenerateNumbers();

                foreach (byte b in aryResult)
                    Console.Write(string.Format("{0:00}  ", b));
            }



            Console.ReadKey();
        }
    }
}
