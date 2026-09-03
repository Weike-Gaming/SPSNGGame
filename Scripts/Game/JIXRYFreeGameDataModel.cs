using System.Linq;
using Weike.MachineInterface;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYFreeGameDataModel : WkFreeGameDataModel
    {
        /// <summary>
        /// int[35]
        /// </summary>
        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public uint[] fgIngotValue
        {
            get => _fgIngotValue;
            set
            {
                if (_fgIngotValue.SequenceEqual(value)) return;
                _fgIngotValue = value.ToArray();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// int[35]
        /// </summary>
        public uint[] fgTempIngotValue
        {
            get => _fgTempIngotValue;
            set
            {
                if (_fgTempIngotValue.SequenceEqual(value))
                {
                    return;
                }

                _fgTempIngotValue = value.ToArray();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// int[35]
        /// </summary>
        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public uint[] fgPreviousIngotValue
        {
            get => _fgPreviousIngotValue;
            set
            {
                if (_fgPreviousIngotValue.SequenceEqual(value))
                {
                    return;
                }

                _fgPreviousIngotValue = value.ToArray();
                OnPropertyChanged();
            }
        }

        #region Extra prize

        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public uint extraPrizeMultiplier
        {
            get => _extraPrizeMultiplier;
            set
            {
                if (_extraPrizeMultiplier == value) return;
                _extraPrizeMultiplier = value;
                OnPropertyChanged();
            }
        }

        // Backing private is a single uint; expose as uint to match.
        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public uint extraPrizeMultiplierIngotValue
        {
            get => _extraPrizeMultiplierIngotValue;
            set
            {
                if (_extraPrizeMultiplierIngotValue == value) return;
                _extraPrizeMultiplierIngotValue = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public uint previousExtraPrizeMultiplier
        {
            get => _previousExtraPrizeMultiplier;
            set
            {
                if (_previousExtraPrizeMultiplier == value)
                {
                    return;
                }
                _previousExtraPrizeMultiplier = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Jackpot Ingot
        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public byte extraJackpotType
        {
            get => _extraJackpotType;
            set
            {
                if (_extraJackpotType == value)
                {
                    return;
                }
                _extraJackpotType = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public byte previousExtraJackpotType
        {
            get => _previousExtraJackpotType;
            set
            {
                if (_previousExtraJackpotType == value)
                {
                    return;
                }
                _previousExtraJackpotType = value;
                OnPropertyChanged();
            }
        }
        #endregion


        #region Reel Nudge
        public bool isLuckyWin
        {
            get => _isLuckyWin;
            set
            {
                if (_isLuckyWin == value)
                {
                    return;
                }
                _isLuckyWin = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private uint[] _fgIngotValue = new uint[40];
        private uint[] _fgTempIngotValue = new uint[40];
        private uint[] _fgPreviousIngotValue = new uint[40];

        private uint _extraPrizeMultiplier = 0;
        private uint _extraPrizeMultiplierIngotValue = 0;
        private uint _previousExtraPrizeMultiplier = 0;

        private byte _extraJackpotType = 0;
        private byte _previousExtraJackpotType = 0;

        private bool _isLuckyWin;
    }
}