using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYWinManagerModel : WkSlotWinManagerModel
    {
        private long _totalFgIngotWinAmount;
        /// <summary>
        /// Total for that particular spin
        /// </summary>
        public long totalFgIngotWinAmount
        {
            get => _totalFgIngotWinAmount;
            set
            {
                if (value == _totalFgIngotWinAmount) return;
                _totalFgIngotWinAmount = value;
                OnPropertyChanged();
            }
        }

        private long _totalMgIngotWinAmount;
        /// <summary>
        /// Total for that particular spin
        /// </summary>
        public long totalMgIngotWinAmount
        {
            get => _totalMgIngotWinAmount;
            set
            {
                if (value == _totalMgIngotWinAmount) return;
                _totalMgIngotWinAmount = value;
                OnPropertyChanged();
            }
        }

        private long _fgFirstIncrementAmount;
        public long fgFirstIncrementAmount
        {
            get => _fgFirstIncrementAmount;
            set
            {
                if(_fgFirstIncrementAmount == value) return;
                _fgFirstIncrementAmount = value;
                OnPropertyChanged();
            }
        }
    }
}