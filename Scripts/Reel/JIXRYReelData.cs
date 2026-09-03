using System.Linq;
using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYReelData : WkReelData
    {
        public bool haveMultiplierIngot
        {
            get => _haveMultiplierIngot;
            set
            {
                if (value == _haveMultiplierIngot)
                {
                    return;
                }

                _haveMultiplierIngot = value;
                OnPropertyChanged();
            }
        }

        public uint[] reelIngotValue
        {
            get => _reelIngotValue;
            set
            {
                if (_reelIngotValue.SequenceEqual(value))
                {
                    return;
                }

                _reelIngotValue = value;
                OnPropertyChanged();
            }
        }

        public bool possibleJackpotIngotWin
        {
            get => _possibleJackpotIngotWin;
            set
            {
                if (value == _possibleJackpotIngotWin)
                {
                    return;
                }

                _possibleJackpotIngotWin = value;
                OnPropertyChanged();
            }
        }

        public byte extraPrizeMultiplierType
        {
            get => _extraPrizeMultiplierType;
            set
            {
                if (value == _extraPrizeMultiplierType)
                {
                    return;
                }

                _extraPrizeMultiplierType = value;
                OnPropertyChanged();
            }
        }

        public byte extraJackpotType
        {
            get => _extraJackpotType;
            set
            {
                if (value == _extraJackpotType)
                {
                    return;
                }

                _extraJackpotType = value;
                OnPropertyChanged();
            }
        }

        public Transform animationTransform
        {
            get => _animationTransform;
            set
            {
                if (value == _animationTransform)
                {
                    return;
                }

                _animationTransform = value;
                OnPropertyChanged();
            }
        }

        public byte animationBitmask
        {
            get => _animationBitmask;
            set
            {
                if (value == _animationBitmask)
                {
                    return;
                }

                _animationBitmask = value;
                OnPropertyChanged();
            }
        }
        
        public bool updateFgCheatData
        {
            get => _updateFgCheatData;
            set
            {
                if (value == _updateFgCheatData)
                {
                    return;
                }

                _updateFgCheatData = value;
                OnPropertyChanged();
            }
        }

        public bool getAnimationData
        {
            get => _getAnimationData;
            set
            {
                if (value == _getAnimationData)
                {
                    return;
                }

                _getAnimationData = value;
                OnPropertyChanged();
            }
        }


        public bool playIngotTransformation
        {
            get => _playIngotTransformation;
            set
            {
                if (value == _playIngotTransformation)
                {
                    return;
                }

                _playIngotTransformation = value;
                OnPropertyChanged();
            }
        }

        public bool playExtraPrizeMultiplierAnimation
        {
            get => _playExtraPrizeMultiplierAnimation;
            set
            {
                if (value == _playExtraPrizeMultiplierAnimation)
                {
                    return;
                }

                _playExtraPrizeMultiplierAnimation = value;
                OnPropertyChanged();
            }
        }

        public bool playExtraPrizeMultiplierTransformation
        {
            get => _playExtraPrizeMultiplierTransformation;
            set
            {
                if (value == _playExtraPrizeMultiplierTransformation)
                {
                    return;
                }

                _playExtraPrizeMultiplierTransformation = value;
                OnPropertyChanged();
            }
        }

        public bool playExtraPrizeMultiplierTransformationWithoutAnimation
        {
            get => _playExtraPrizeMultiplierTransformationWithoutAnimation;
            set
            {
                if (value == _playExtraPrizeMultiplierTransformationWithoutAnimation)
                {
                    return;
                }

                _playExtraPrizeMultiplierTransformationWithoutAnimation = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// bool To disable the reel's border
        /// Used on StateFreeGameReelNudge.OnExit by Reelmanager
        /// </summary>
        public bool playReelNudgeBorderExitAnim
        {
            get => _playReelNudgeBorderExitAnim;
            set
            {
                if (value == _playReelNudgeBorderExitAnim)
                {
                    return;
                }

                _playReelNudgeBorderExitAnim = value;
                OnPropertyChanged();
            }
        }

        public bool updatePreviousIngotData
        {
            get => _updatePreviousIngotData;
            set
            {
                if (value == _updatePreviousIngotData)
                {
                    return;
                }

                _updatePreviousIngotData = value;
                OnPropertyChanged();
            }
        }

        public bool updateRecoverData
        {
            get => _updateRecoverData;
            set
            {
                if (value == _updateRecoverData)
                {
                    return;
                }

                _updateRecoverData = value;
                OnPropertyChanged();
            }
        }

        public bool updateRecoverTransform
        {
            get => _updateRecoverTransform;
            set
            {
                if (value == _updateRecoverTransform)
                {
                    return;
                }

                _updateRecoverTransform = value;
                OnPropertyChanged();
            }
        }

        public bool resetTransformedSymbol
        {
            get => _resetTransformedSymbol;
            set
            {
                if (value == _resetTransformedSymbol)
                {
                    return;
                }
                _resetTransformedSymbol = value;
                OnPropertyChanged();
            }
        }

        public bool isLuckyBoost
        {
            get => _isLuckyBoost;
            set
            {
                if (value == _isLuckyBoost)
                {
                    return;
                }
                _isLuckyBoost = value;
                OnPropertyChanged();
            }
        }
        public bool noLuckyBoost
        {
            get => _noLuckyBoost;
            set
            {
                if (value == _noLuckyBoost)
                {
                    return;
                }
                _noLuckyBoost = value;
                OnPropertyChanged();
            }
        }
        #region Reel Nudge

        /// <summary>
        /// Num of rows
        /// </summary>
        public int numDummy
        {
            get => _numDummy;
            set
            {
                _numDummy = value;
                OnPropertyChanged();
            }
        }

        public bool wantToNudgeInDefaultDir
        {
            get => _wantToNudgeInDefaultDir;
            set
            {
                if (_wantToNudgeInDefaultDir == value) return;
                _wantToNudgeInDefaultDir = value;
                OnPropertyChanged();
            }
        }

        public int nudgeSteps
        {
            get => _nudgeSteps;
            set
            {
                if (_nudgeSteps == value) return;
                _nudgeSteps = value;
                OnPropertyChanged();
            }
        }

        public bool wantToPlayNudgeAnim
        {
            get => _wantToPlayNudgeAnim;
            set
            {
                 if (_wantToPlayNudgeAnim == value) return;
                _wantToPlayNudgeAnim = value;
                OnPropertyChanged();
            }
        }

        public bool redrawSymbols
        {
            get => _redrawSymsbols;
            set
            {
                if (_redrawSymsbols == value) return;
                _redrawSymsbols = value;
                OnPropertyChanged();
            }
        }

        [SerializeField] private int _numDummy = 2;
        private bool _wantToNudgeInDefaultDir = false;
        private int _nudgeSteps = 0;
        private bool _wantToPlayNudgeAnim = false;
        #endregion

        private bool _haveMultiplierIngot = false;
        private uint[] _reelIngotValue = new uint[8];
        private bool _possibleJackpotIngotWin = false;
        private byte _extraPrizeMultiplierType = 0;
        private byte _extraJackpotType = 0;

        private Transform _animationTransform = null;
        private byte _animationBitmask = 0;
        private bool _updateFgCheatData = false;
        private bool _getAnimationData = false;
        private bool _playIngotTransformation = false;
        private bool _playExtraPrizeMultiplierAnimation = false;
        private bool _playExtraPrizeMultiplierTransformation = false;

        private bool _updatePreviousIngotData = false;
        private bool _updateRecoverData = false;
        private bool _updateRecoverTransform = false;
        private bool _resetTransformedSymbol = false;

        private bool _playReelNudgeBorderExitAnim = false;

        // History
        private bool _playExtraPrizeMultiplierTransformationWithoutAnimation = false;
        private bool _redrawSymsbols = false;

        private bool _isLuckyBoost;
        private bool _noLuckyBoost;
    }
}