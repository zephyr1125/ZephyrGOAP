using System;

namespace DOTS.Game
{
    public class GameTime
    {
        private static GameTime _instance;

        public static GameTime Instance()
        {
            if (_instance == null)
            {
                _instance = new GameTime();
            }

            return _instance;
        }
        
        public float TimeScale;
        public double Second;
        public float DeltaSecond;

        private float _prevTimeScale;

        public void Pause()
        {
            if (Math.Abs(TimeScale) < 0.01f) return;
            _prevTimeScale = TimeScale;
            TimeScale = 0;
        }

        public void Resume()
        {
            TimeScale = _prevTimeScale;
        }
    }
}