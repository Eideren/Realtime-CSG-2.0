namespace RealtimeCSG.Foundation
{
    public struct Version
    {
        public byte Major { get; set; }
        public byte Minor { get; set; }
        public byte Revision { get; set; }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }
    }
}
