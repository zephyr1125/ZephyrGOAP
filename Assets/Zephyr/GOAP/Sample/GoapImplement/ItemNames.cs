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
            if(_instance.RawPeachName.Equals(default))
            {
                Init();
            }
            return _instance;
        }

        public NativeString32 RawPeachName;
        public NativeString32 RoastPeachName;
        public NativeString32 RawAppleName;
        public NativeString32 RoastAppleName;
        public NativeString32 FeastName;

        
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