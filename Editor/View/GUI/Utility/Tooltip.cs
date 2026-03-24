namespace RealtimeCSG
{
    internal class ToolTip
    {
        public ToolTip(string title, string contents)
        {
            this.title    = title;
            this.contents = contents;
        }

        public ToolTip(string title, string contents, KeyEvent key)
        {
            this.title    = title;
            this.contents = contents;
            this.key      = key;
        }

        public readonly string title;
        public readonly string contents;
        public readonly KeyEvent key;

        public string TitleString() => $"<size=13><color=white><b>{title}</b></color></size>";
        public string ContentsString() => $"<size=12><color=white>{contents}</color></size>";

        public string KeyString()
        {
            if(key == null || key.IsEmpty())
            {
                return string.Empty;
            }

            return $"<color=white><size=11><b>hotkey:</b> <i>{key}</i></size></color>";
        }

        public override string ToString() => TitleString() + ContentsString() + KeyString();
    }
}
