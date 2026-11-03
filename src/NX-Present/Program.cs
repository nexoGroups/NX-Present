using System;
using Velopack;

namespace NXPresent;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Must run first: when Setup or the updater start this exe with their hook arguments, Velopack handles them and exits.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
