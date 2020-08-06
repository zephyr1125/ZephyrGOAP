using Unity.Collections;

namespace Zephyr.GOAP.Sample.GoapImplement
{
    public enum ItemName
    {
        RawPeach, RoastPeach, RawApple, RoastApple, Feast
    }
    public struct ItemNames
    {
        private static ItemNames _instance;
        
        public static ItemNames Instance()
        {
            if(_instance.RawPeachName.Equals(new FixedString32()))
            {
                Init();
            }
            return _instance;
        }

        public FixedString32 RawPeachName;
        public FixedString32 RoastPeachName;
        public FixedString32 RawAppleName;
        public FixedString32 RoastAppleName;
        public FixedString32 FeastName;

        
        public static void Init()
        {
            _instance = new ItemNames
            {
                RawPeachName = "raw_peach",
                RoastPeachName = "roast_peach",
                RawAppleName = "raw_apple",
                RoastAppleName = "roast_apple",
                FeastName = "feast",
                
            };
        }
    }
}