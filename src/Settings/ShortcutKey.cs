using System;
namespace MW.EditorTools.Settings
{
    internal static class ShortcutKey
    {
        // Explicit keyboard paths also accepted by Unity InputSystem.Key (case-insensitive).
        internal static bool TryNormalize(string value, out string key)
        {
            key=(value??"").Trim().ToUpperInvariant();
            if(key.Length==1&&key[0]>='A'&&key[0]<='Z')return true;
            int number;
            if(key.StartsWith("F",StringComparison.Ordinal)&&int.TryParse(key.Substring(1),out number)&&number>=1&&number<=12&&key=="F"+number)return true;
            if(key.Length==6&&key.StartsWith("DIGIT",StringComparison.Ordinal)&&key[5]>='0'&&key[5]<='9')return true;
            switch(key) {
                case "HOME": case "END": case "INSERT": case "DELETE": case "PAGEUP": case "PAGEDOWN":
                case "SPACE": case "TAB": case "ENTER": case "BACKSPACE": case "BACKQUOTE":
                case "UPARROW": case "DOWNARROW": case "LEFTARROW": case "RIGHTARROW":
                case "MINUS": case "EQUALS": case "LEFTBRACKET": case "RIGHTBRACKET":
                case "SEMICOLON": case "QUOTE": case "BACKSLASH": case "COMMA": case "PERIOD": case "SLASH":
                    return true;
                default:return false;
            }
        }
    }
}
