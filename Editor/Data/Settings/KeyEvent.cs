using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal struct KeyCodeWithModifier
    {
        public KeyCodeWithModifier(KeyCode code, EventModifiers modifier = EventModifiers.None, bool hold = false)
        {
            keyCode   = code;
            modifiers = modifier;
            this.hold = hold;
        }

        public KeyCode keyCode;
        public EventModifiers modifiers;
        public readonly bool hold;

        public bool IsKeyPressed()
        {
            return Event.current.keyCode == keyCode
             && (Event.current.modifiers & ~EventModifiers.FunctionKey) == modifiers;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if((modifiers & EventModifiers.Command) > 0)
            {
                if(builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append("Command");
            }

            if((modifiers & EventModifiers.Control) > 0)
            {
                if(builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append("Control");
            }

            if((modifiers & EventModifiers.Alt) > 0)
            {
                if(builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append("Alt");
            }

            if((modifiers & EventModifiers.Shift) > 0)
            {
                if(builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append("Shift");
            }

            if(builder.Length > 0)
            {
                builder.Append('+');
            }

            builder.Append(KeyEvent.CodeToString(keyCode));

            if(hold)
            {
                builder.Append(" (hold)");
            }

            return builder.ToString();
        }
    }

    // TODO: ability to connect "keyEvent" with delegate
    // TODO: ability to connect list of "keyevent"/"delegate" combo with controlid

    internal class KeyEvent
    {
        public KeyEvent(KeyCode code)
        {
            keys        = new[] { new KeyCodeWithModifier(code) };
            defaultKeys = keys;
        }

        public KeyEvent(KeyCode code, bool hold)
        {
            keys        = new[] { new KeyCodeWithModifier(code, EventModifiers.None, hold) };
            defaultKeys = keys;
        }

        public KeyEvent(KeyCode code, EventModifiers modifier)
        {
            keys        = new[] { new KeyCodeWithModifier(code, modifier) };
            defaultKeys = keys;
        }

        public KeyEvent(KeyCode code, EventModifiers modifier, bool hold)
        {
            keys        = new[] { new KeyCodeWithModifier(code, modifier, hold) };
            defaultKeys = keys;
        }

        public KeyEvent(params KeyCode[] keyCodes)
        {
            keys = new KeyCodeWithModifier[keyCodes.Length];

            for(int i = 0; i < keyCodes.Length; i++)
            {
                keys[i] = new KeyCodeWithModifier(keyCodes[i]);
            }

            defaultKeys = keys;
        }

        public KeyEvent(params KeyCodeWithModifier[] keys)
        {
            this.keys        = keys;
            defaultKeys = keys;
        }

        public readonly KeyCodeWithModifier[] defaultKeys;
        public KeyCodeWithModifier[] keys;

        public bool IsEmpty()
        {
            return keys == null || keys.Length == 0;
        }

        public bool IsKeyPressed()
        {
            if(!CSGSettings.EnableRealtimeCSG)
            {
                return false;
            }

            for(int i = 0; i < keys.Length; i++)
            {
                if(keys[i].IsKeyPressed())
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            if(keys.Length == 0)
            {
                return string.Empty;
            }

            if(keys.Length == 1)
            {
                return keys[0].ToString();
            }

            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < keys.Length; i++)
            {
                if(i > 0)
                {
                    builder.Append(" or ");
                }

                builder.Append(keys[i].ToString());
            }

            return builder.ToString();
        }

        public static string CodeToString(KeyCode code)
        {
            return code switch
            {
                KeyCode.Exclaim        => "!"
              , KeyCode.DoubleQuote    => "\""
              , KeyCode.Hash           => "#"
              , KeyCode.Dollar         => "$"
              , KeyCode.Ampersand      => "&"
              , KeyCode.Quote          => "\'"
              , KeyCode.LeftParen      => "("
              , KeyCode.RightParen     => ")"
              , KeyCode.Asterisk       => "*"
              , KeyCode.Plus           => "+"
              , KeyCode.Comma          => ","
              , KeyCode.Minus          => "-"
              , KeyCode.Period         => "."
              , KeyCode.Slash          => "/"
              , KeyCode.Alpha0         => "0"
              , KeyCode.Alpha1         => "1"
              , KeyCode.Alpha2         => "2"
              , KeyCode.Alpha3         => "3"
              , KeyCode.Alpha4         => "4"
              , KeyCode.Alpha5         => "5"
              , KeyCode.Alpha6         => "6"
              , KeyCode.Alpha7         => "7"
              , KeyCode.Alpha8         => "8"
              , KeyCode.Alpha9         => "9"
              , KeyCode.Colon          => ":"
              , KeyCode.Semicolon      => ";"
              , KeyCode.Less           => "<"
              , KeyCode.Equals         => "="
              , KeyCode.Greater        => ">"
              , KeyCode.Question       => "?"
              , KeyCode.At             => "@"
              , KeyCode.LeftBracket    => "["
              , KeyCode.Backslash      => "\\"
              , KeyCode.RightBracket   => "]"
              , KeyCode.Caret          => "^"
              , KeyCode.Underscore     => "_"
              , KeyCode.BackQuote      => "`"
              , KeyCode.KeypadPeriod   => "Keypad '.'"
              , KeyCode.KeypadDivide   => "Keypad '/'"
              , KeyCode.KeypadMultiply => "Keypad '*'"
              , KeyCode.KeypadMinus    => "Keypad '-'"
              , KeyCode.KeypadPlus     => "Keypad '+'"
              , KeyCode.KeypadEquals   => "Keypad '='"
              , _                      => ObjectNames.NicifyVariableName(code.ToString())
            };
        }
    }

    internal class KeyDescription : Attribute
    {
        public KeyDescription(string name)
        {
            this.name = name;
        }

        public readonly string name;
    }
}
