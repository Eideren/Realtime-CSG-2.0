using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEditor;
using UnityEditor.ShortcutManagement;
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
        public KeyEvent(string shortcutId, [AllowNull] Keys.IConsumableShortcutContext context, bool hold = false, [AllowNull] params KeyCodeWithModifier[] forcedKeys)
        {
            this.context = context;
            this.hold = hold;
            this.shortcutId = shortcutId;
            this.forcedKeys = forcedKeys ?? Array.Empty<KeyCodeWithModifier>();
            ShortcutManager.instance.shortcutBindingChanged += (arg) => ReloadShortcut();
            ReloadShortcut();
        }

        private void ReloadShortcut()
        {
            var agg = new List<KeyCodeWithModifier>(forcedKeys);
            if (shortcutId != null)
            {
                foreach (var seq in ShortcutManager.instance.GetShortcutBinding(shortcutId).keyCombinationSequence)
                {
                    var modifiers = EventModifiers.None;
                    if ((seq.modifiers & ShortcutModifiers.Alt) != 0)
                        modifiers |= EventModifiers.Alt;
                    if ((seq.modifiers & ShortcutModifiers.Action) != 0)
                        modifiers |= EventModifiers.Control | EventModifiers.Command;
                    if ((seq.modifiers & ShortcutModifiers.Control) != 0)
                        modifiers |= EventModifiers.Control;
                    if ((seq.modifiers & ShortcutModifiers.Shift) != 0)
                        modifiers |= EventModifiers.Shift;

                    agg.Add(new KeyCodeWithModifier(seq.keyCode, modifiers, hold));
                }
            }

            keys = agg.ToArray();
        }

        [AllowNull, MaybeNull] private readonly Keys.IConsumableShortcutContext context;
        private readonly bool hold;
        [AllowNull, MaybeNull] private readonly string shortcutId;
        private KeyCodeWithModifier[] forcedKeys;
        private KeyCodeWithModifier[] keys = Array.Empty<KeyCodeWithModifier>();

        public bool IsEmpty()
        {
            return keys.Length == 0;
        }

        public bool IsKeyPressed(bool consume = true)
        {
            if(context is not null && !context.CanRaiseEvent)
            {
                return false;
            }

            for(int i = 0; i < keys.Length; i++)
            {
                if(keys[i].IsKeyPressed())
                {
                    if (consume)
                        context?.Consume();
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
              , var _                  => ObjectNames.NicifyVariableName(code.ToString())
            };
        }
    }
}
