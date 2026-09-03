using System;
using System.Linq;
using Weike.MachineInterface;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYGameDataModel : WkSlotGameDataModel
    {
        /// <summary>
        /// bool to enable help page in FgSelection state
        /// </summary>
        public bool enableSelectionHelpPage
        {
            get => _enableSelectionHelpPage;
            set
            {
                if (_enableSelectionHelpPage == value)
                    return;

                _enableSelectionHelpPage = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        public byte previousMaxWayWin
        {
            get => _previousMaxWayWin;
            set
            {
                if (_previousMaxWayWin == value)
                {
                    return;
                }
                _previousMaxWayWin = value;
                OnPropertyChanged();
            }
        }

        public byte maxIngotWayWin
        {
            get => _maxIngotWayWin;
            set
            {
                if (value == _maxIngotWayWin)
                {
                    return;
                }

                _maxIngotWayWin = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToHistory(WkHistorySaveType.MainGame)]
        [WkSaveToSram]
        public uint[] ingotValue
        {
            get => _ingotValue;
            set
            {
                if (_ingotValue.SequenceEqual(value))
                {
                    return;
                }

                _ingotValue = value.ToArray();
                OnPropertyChanged();
            }
        }
 

        [WkSaveToSram]
        public uint[] previousIngotValue
        {
            get => _previousIngotValue;
            set
            {
                if (_previousIngotValue.SequenceEqual(value))
                {
                    return;
                }

                _previousIngotValue = value.ToArray();
                OnPropertyChanged();
            }
        }

        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public byte potFeatureGameFlag
        {
            get => _potFeatureGameFlag;
            set
            {
                if (value == _potFeatureGameFlag)
                {
                    return;
                }

                _potFeatureGameFlag = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public byte upcomingPotFeatureGameFlag
        {
            get => _upcomingPotFeatureGameFlag;
            set
            {
                if (value == _upcomingPotFeatureGameFlag)
                {
                    return;
                }

                _upcomingPotFeatureGameFlag = value;
                OnPropertyChanged();
            }
        }
        
        public byte previousPotFeatureGameFlag
        {
            get => _previousPotFeatureGameFlag;
            set
            {
                if (value == _previousPotFeatureGameFlag)
                {
                    return;
                }

                _previousPotFeatureGameFlag = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToHistory(WkHistorySaveType.SubGame)]
        [WkSaveToSram]
        public byte savedPreviousPotFeatureGameFlag
        {
            get => _savedPreviousPotFeatureGameFlag;
            set
            {
                if (value == _savedPreviousPotFeatureGameFlag)
                {
                    return;
                }

                _savedPreviousPotFeatureGameFlag = value;
                _previousPotFeatureGameFlag = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Save the pot feature flag that triggers Free Game.
        /// Used to play scatter animation in idle & history main game
        /// </summary>
        [WkSaveToHistory(WkHistorySaveType.MainGame)]
        [WkSaveToSram]
        public byte savedTriggerPotFeatureGameFlag
        {
            get => _savedTriggerPotFeatureGameFlag;
            set
            {
                if (value == _savedTriggerPotFeatureGameFlag)
                {
                    return;
                }

                _savedTriggerPotFeatureGameFlag = value;
                OnPropertyChanged();
            }
        }

        public bool shouldPlayHitFeatureAnim
        {
            get => _shouldPlayHitFeatureAnim;
            set
            {
                if (_shouldPlayHitFeatureAnim == value)
                {
                    return;
                }
                _shouldPlayHitFeatureAnim = value;
                OnPropertyChanged();
            }
        }

        public bool playHitFeatureAnim
        {
            get => _playHitFeatureAnim;
            set
            {
                if (_playHitFeatureAnim == value)
                {
                    return;
                }
                _playHitFeatureAnim = value;
                OnPropertyChanged();
            }
        }

        public int tempBluePotScatter
        {
            get => _tempBluePotScatter;
            set
            {
                if (_tempBluePotScatter == value)
                {
                    return;
                }
                _tempBluePotScatter = value;
                OnPropertyChanged();
            }
        }

        public int tempRedPotScatter
        {
            get => _tempRedPotScatter;
            set
            {
                if (_tempRedPotScatter == value)
                {
                    return;
                }
                _tempRedPotScatter = value;
                OnPropertyChanged();
            }
        }

        public int tempGreenPotScatter
        {
            get => _tempGreenPotScatter;
            set
            {
                if (_tempGreenPotScatter == value)
                {
                    return;
                }
                _tempGreenPotScatter = value;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public int savedBluePotScatter
        {
            get => _savedBluePotScatter;
            set
            {
                if (_savedBluePotScatter == value)
                {
                    return;
                }
                _savedBluePotScatter = value;
                tempBluePotScatter = _savedBluePotScatter;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public int savedRedPotScatter
        {
            get => _savedRedPotScatter;
            set
            {
                if (_savedRedPotScatter == value)
                {
                    return;
                }
                _savedRedPotScatter = value;
                tempRedPotScatter = _savedRedPotScatter;
                OnPropertyChanged();
            }
        }

        [WkSaveToSram]
        public int savedGreenPotScatter
        {
            get => _savedGreenPotScatter;
            set
            {
                if (_savedGreenPotScatter == value)
                {
                    return;
                }
                _savedGreenPotScatter = value;
                tempGreenPotScatter = _savedGreenPotScatter;
                OnPropertyChanged();
            }
        }

        public bool playFeatureTriggerCoinAnim
        {
            get => _playFeatureTriggerCoinAnim;
            set
            {
                if (_playFeatureTriggerCoinAnim == value)
                {
                    return;
                }
                _playFeatureTriggerCoinAnim = value;
                OnPropertyChanged();
            }
        }

        public bool extremeBigWinPreSpinAnim
        {
            get => _extremeBigWinPreSpinAnim;
            set
            {
                if (value == _extremeBigWinPreSpinAnim)
                {
                    return;
                }

                _extremeBigWinPreSpinAnim = value;
                OnPropertyChanged();
            }
        }

        public bool blueCoinInAnim
        {
            get => _blueCoinInAnim;
            set
            {
                if (value == _blueCoinInAnim)
                {
                    return;
                }

                _blueCoinInAnim = value;
                OnPropertyChanged();
            }
        }

        public bool redCoinInAnim
        {
            get => _redCoinInAnim;
            set
            {
                if (value == _redCoinInAnim)
                {
                    return;
                }

                _redCoinInAnim = value;
                OnPropertyChanged();
            }
        }

        public bool greenCoinInAnim
        {
            get => _greenCoinInAnim;
            set
            {
                if (value == _greenCoinInAnim)
                {
                    return;
                }

                _greenCoinInAnim = value;
                OnPropertyChanged();
            }
        }

        public bool triggerFinalAnim
        {
            get => _triggerFinalAnim;
            set
            {
                if (_triggerFinalAnim == value) return;
                _triggerFinalAnim = value;
                OnPropertyChanged();
            }
        }

        // Help page UI
        private bool _enableSelectionHelpPage = false;

        private uint[] _ingotValue = new uint[40];       
        private uint[] _previousIngotValue = new uint[40];
        
        private byte _potFeatureGameFlag = 0;
        private byte _upcomingPotFeatureGameFlag = 0;
        private byte _previousPotFeatureGameFlag = 0;
        private byte _savedPreviousPotFeatureGameFlag = 0;
        private byte _savedTriggerPotFeatureGameFlag = 0;

        private bool _shouldPlayHitFeatureAnim = false;
        private bool _playHitFeatureAnim = false;

        private int _tempBluePotScatter = 0;
        private int _tempRedPotScatter = 0;
        private int _tempGreenPotScatter = 0;
        private int _savedBluePotScatter = 0;
        private int _savedRedPotScatter = 0;
        private int _savedGreenPotScatter = 0;
        private bool _playFeatureTriggerCoinAnim;

        private bool _extremeBigWinPreSpinAnim = false;
        private byte _previousMaxWayWin = 0;
        private byte _maxIngotWayWin = 0;

        private bool _blueCoinInAnim = false;
        private bool _redCoinInAnim = false;
        private bool _greenCoinInAnim = false;

        private bool _triggerFinalAnim = false;
    }
}