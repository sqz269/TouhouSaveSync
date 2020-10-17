using System;
using System.Collections.Generic;
using System.Text;

namespace TouhouSaveSync.Utility
{
    static class InputManager
    {
        public static string GetStringInput(string message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }
    }
}
