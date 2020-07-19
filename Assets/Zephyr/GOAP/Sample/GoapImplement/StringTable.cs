using System;
using Unity.Collections;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;

namespace Zephyr.GOAP.Sample.GoapImplement
{
    public struct StringTable
    {
        private static readonly StringTable _instance;

        static StringTable()
        {
            _instance = new StringTable();
        }
        
        public static StringTable Instance()
        {
            return _instance;
        }

        public NativeString32 RawPeachName;
        public NativeString32 RoastPeachName;
        public NativeString32 RawAppleName;
        public NativeString32 RoastAppleName;
        public NativeString32 FeastName;
        
        public NativeString32 CollectActionName;
        public NativeString32 CookActionName;
        public NativeString32 DropItemActionName;
        public NativeString32 DropRawActionName;
        public NativeString32 EatActionName;
        public NativeString32 PickItemActionName;
        public NativeString32 PickRawActionName;
        public NativeString32 WanderActionName;

        
        public void Init()
        {
            RawPeachName = "raw_peach";
            RoastPeachName = "roast_peach";
            RawAppleName = "raw_apple";
            RoastAppleName = "roast_apple";
            FeastName = "feast";
            
            CollectActionName = nameof(CollectAction);
            CookActionName = nameof(CookAction);
            DropItemActionName = nameof(DropItemAction);
            DropRawActionName = nameof(DropRawAction);
            EatActionName = nameof(EatAction);
            PickItemActionName = nameof(PickItemAction);
            PickRawActionName = nameof(PickRawAction);
            WanderActionName = nameof(WanderAction);
        }
    }
}