using System;
using MonoDevelop.Components.Commands;

namespace IntroductionToRoslyn
{
    public class StartupHandler : CommandHandler
    {
        protected override void Run()
        {
			Console.WriteLine("Introduction to Roslyn Addin is starting up!");
			Console.WriteLine("We inject a startup handler so the VS Mac loads our diagnostics and code actions");
        }
    }
}
