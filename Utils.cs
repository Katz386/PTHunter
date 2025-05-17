namespace PTHunter
{
    internal class Utils
    {
        public static void WriteLine(string text, WType type) 
        {
            Console.Write("[");
            switch (type) 
            {
                case WType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("X");
                    break;
                case WType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("!");
                    break;
                case WType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("+");
                    break;
                case WType.Info:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("?");
                    break;
            }
            Console.ResetColor();
            Console.Write($"] {text}{Environment.NewLine}");
        }

        public static void Write(string text, WType type)
        {
            Console.Write("[");
            switch (type)
            {
                case WType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("X");
                    break;
                case WType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("!");
                    break;
                case WType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("+");
                    break;
                case WType.Info:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("?");
                    break;
            }
            Console.ResetColor();
            Console.Write($"] {text}");
        }

        public static void WriteColoredLine(string text, WType type) 
        {
            switch (type)
            {
                case WType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(text);
                    break;
                case WType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                    break;
                case WType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(text);
                    break;
                case WType.Info:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(text);
                    break;
            }
            Console.ResetColor();
        }
    }
}
