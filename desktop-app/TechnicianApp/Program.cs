using System;
using Avalonia;

namespace TechnicianApp
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Check if launched via custom protocol: yourapp://connect?id=123456789
            if (args.Length > 0)
            {
                string arg = args[0];
                if (arg.StartsWith("yourapp://connect?id=", StringComparison.OrdinalIgnoreCase))
                {
                    App.PendingSessionId = arg.Substring("yourapp://connect?id=".Length);
                }
                else if (arg.Length >= 6 && long.TryParse(arg, out _))
                {
                    App.PendingSessionId = arg;
                }
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
