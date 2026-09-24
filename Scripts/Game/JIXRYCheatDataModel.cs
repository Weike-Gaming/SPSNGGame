using System.Linq;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatDataModel : WkCheatDataModel
    {
        public byte demoPotFeature
        {
            get => _demoPotFeature;
            set
            {
                if (_demoPotFeature == value)
                    return;
                _demoPotFeature = value;
                OnPropertyChanged();
            }
        }

        public uint[] predetermineIngotValue
        {
            get => _predetermineIngotValue;
            set
            {
                if (_predetermineIngotValue.SequenceEqual(value))
                    return;
                _predetermineIngotValue = value.ToArray();
                OnPropertyChanged();
            }
        }
        public uint[] predetermineIsMultiply
        {
            get => _predetermineIsMultiply;
            set
            {
                if (_predetermineIsMultiply.SequenceEqual(value))
                    return;
                _predetermineIsMultiply = value.ToArray();
                OnPropertyChanged();
            }
        }
        public byte predetermineJackpotType
        {
            get => _predetermineJackpotType;
            set
            {
                if (_predetermineJackpotType == value)
                    return;
                _predetermineJackpotType = value;
                OnPropertyChanged();
            }
        }

        public byte predetermineExtraPrizeMultiplierType
        {
            get => _predetermineExtraPrizeMultiplierType;
            set
            {
                if (_predetermineExtraPrizeMultiplierType == value)
                    return;
                _predetermineExtraPrizeMultiplierType = value;
                OnPropertyChanged();
            }
        }

        public bool demoEnable
        {
            get => _demoEnable;
            set
            {
                if (_demoEnable == value)
                    return;
                _demoEnable = value;
                OnPropertyChanged();
            }
        }

        private byte _demoPotFeature = 0;
        private uint[] _predetermineIngotValue = new uint[40];
        private uint[] _predetermineIsMultiply = new uint[20];
        private byte _predetermineJackpotType = 1;
        private byte _predetermineExtraPrizeMultiplierType = 1;
        private bool _demoEnable = false;
    }
}