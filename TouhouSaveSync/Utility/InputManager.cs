using System;

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
