using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYReelManagerDataModel : WkReelManagerDataModel
    {
        /// <summary>
        /// For determining what sound is played in "rmdm.customSfx.Add($"ScatterLandingReel_{rmdm.scatterCount})".
        /// 0 in spin.
        /// ++ when playing scatter sound OnReelStopped().
        /// </summary>
        public int scatterCount
        {
            get => _scatterCount;
            set
            {
                if (value == _scatterCount) return;
                _scatterCount = value;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// Total number of scatter hit in current spin.
        /// Reset then set in UpdateScatterStatement.
        /// ++ when scatter won in UpdateScatterStatement().
        /// Played in fast stop.
        /// </summary>
        public int numOfScatterHitInSpin
        {
            get => _numOfScatterHitInSpin;
            set
            {
                if (value == _numOfScatterHitInSpin) return;
                _numOfScatterHitInSpin = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// float Duration of the reel border moving for animating it
        /// </summary>
        public float durationReelNudgeAnim
        {
            get => _durationReelNudgeAnim;
            set
            {
                if (value == _durationReelNudgeAnim) return;
                _durationReelNudgeAnim = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// float Duration of how long more to wait in StateReelNudge using InvokeMethod(x, durationReelNudgeAnim + durationReelNudgeLinger)
        /// </summary>
        public float durationReelNudgeLinger
        {
            get => _durationReelNudgeLinger;
            set
            {
                if (value == _durationReelNudgeLinger) return;
                _durationReelNudgeLinger = value;
                OnPropertyChanged();
            }
        }

        private int _scatterCount = 0;
        private int _numOfScatterHitInSpin = 0;
        private float _durationReelNudgeAnim = 0.5f;
        private float _durationReelNudgeLinger = 1.5f;
    }
}
