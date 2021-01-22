using System;
using System.Collections.Generic;
using System.Text;

namespace Weather2ConsoleApp
{
    class Utils
    {
        public static void ScrollToTop(int bottom)
        {
            Console.CursorTop = 0;
            Console.CursorVisible = false;
            Console.ReadLine();
            Console.CursorTop = bottom;
            Console.CursorVisible = true;
        }

        public static string GetUnderline(string heading)
        {
            string underline = "";

            foreach (char c in heading.ToCharArray())
            {
                underline += "-";
            }

            return underline;
        }

        public static int SelectFromEnum(int length, string input)
        {
            int inputToInt;

            while (true)
            {
                if (int.TryParse(input, out inputToInt) &&
                    inputToInt > 0 && inputToInt <= length)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Ange nummer 1-{length}");
                    input = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return inputToInt;
        }
    }
}
