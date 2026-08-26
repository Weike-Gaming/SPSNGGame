using System;
using System.Linq;

namespace Weike.Games.JIXRY
{
    public class JIXRYSimDataModel : JIXRYGameDataModel
    {
        /// <summary>
        /// "saved" version of fgdm._fgGameTypeIndex.
        /// Used to override the usual selection flow from using FgSelectionButton with SimulationUI
        /// </summary>
        public ushort simFgGameTypeIndex
        {
            get => _simFgGameTypeIndex;
            set
            {
                if (value == _simFgGameTypeIndex) return;
                _simFgGameTypeIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// "saved" version of fgdm._fgGameTypeIsRandom.
        /// Used to override the usual selection flow from using FgSelectionButton with SimulationUI
        /// </summary>
        public bool simFgGameTypeIsRandom
        {
            get => _simFgGameTypeIsRandom;
            set
            {
                if (value == _simFgGameTypeIsRandom) return;
                _simFgGameTypeIsRandom = value;
                OnPropertyChanged();
            }
        }

        public bool isSimulationStarted
        {
            get => _isSimulationStarted;
            set
            {
                if (value == _isSimulationStarted) return;
                _isSimulationStarted = value;
                OnPropertyChanged();
            }
        }

        public float simElapsedTime
        {
            get => _simElapsedTime;
            set
            {
                if (value.Equals(_simElapsedTime)) return;
                _simElapsedTime = value;
                OnPropertyChanged();
            }
        }

        public ulong totalTopUpAmount
        {
            get => _totalTopUpAmount;
            set
            {
                if (value == _totalTopUpAmount) return;
                _totalTopUpAmount = value;
                OnPropertyChanged();
            }
        }

        public string[] variationNames
        {
            get => _variationNames;
            set
            {
                if (value == null || (variationNames != null && variationNames.SequenceEqual(value))) return;
                _variationNames = value;
                OnPropertyChanged();
            }
        }

        public string[] denomNames
        {
            get => _denomNames;
            set
            {
                if (value == null || (denomNames != null && denomNames.SequenceEqual(value))) return;
                _denomNames = value;
                OnPropertyChanged();
            }
        }

        public bool isInFreeGame
        {
            get => _isInFreeGame;
            set
            {
                if (value == _isInFreeGame) return;
                _isInFreeGame = value;
                OnPropertyChanged();
            }
        }

        public ulong totalBet
        {
            get => _totalBet;
            set
            {
                if (_totalBet == value) return;
                _totalBet = value;
                OnPropertyChanged();
            }
        }

        public ulong totalBetCount
        {
            get => _totalBetCount;
            set
            {
                if (_totalBetCount == value) return;
                _totalBetCount = value;
                OnPropertyChanged();
            }
        }

        public ulong totalWin
        {
            get => _totalWin;
            set
            {
                if (_totalWin == value) return;
                _totalWin = value;
                OnPropertyChanged();
            }
        }

        public ulong totalGameWin
        {
            get => _totalGameWin;
            set
            {
                if (value == _totalGameWin) return;

                ulong prev = _totalGameWin;

                _totalGameWin = value;

                ulong diff = _totalGameWin - prev;
                totalWin += diff;

                if (mgHighestWin < diff)
                {
                    mgHighestWin = diff;
                }

                OnPropertyChanged();
            }
        }

        public ulong totalMgWin
        {
            get => _totalMgWin;
            set
            {
                if (value == _totalMgWin) return;
                _totalMgWin = value;
                OnPropertyChanged();
            }
        }

        public ulong totalScatterWin
        {
            get => _totalScatterWin;
            set
            {
                if (value == _totalScatterWin) return;
                _totalScatterWin = value;
                OnPropertyChanged();
            }
        }

        public ulong totalIconWin
        {
            get => _totalIconWin;
            set
            {
                if (value == _totalIconWin) return;
                _totalIconWin = value;
                OnPropertyChanged();
            }
        }

        public ulong mgHighestWin
        {
            get => _mgHighestWin;
            set
            {
                if (value == _mgHighestWin) return;
                _mgHighestWin = value;
                OnPropertyChanged();
            }
        }

        public ulong singleMgWin
        {
            get => _singleMgWin;
            set
            {
                if (value == _singleMgWin) return;
                _singleMgWin = value;
                OnPropertyChanged();
            }
        }

        public ulong totalFgWin
        {
            get => _totalFgWin;
            set
            {
                if (value == _totalFgWin) return;

                ulong prev = _totalFgWin;

                _totalFgWin = value;

                ulong diff = _totalFgWin - prev;
                totalWin += diff;

                if (fgHighestWin < diff)
                {
                    fgHighestWin = diff;
                }

                OnPropertyChanged();
            }
        }

        public ulong fgHighestWin
        {
            get => _fgHighestWin;
            set
            {
                if (value == _fgHighestWin) return;
                _fgHighestWin = value;
                OnPropertyChanged();
            }
        }

        public ulong totalGamePlayed
        {
            get => _totalGamePlayed;
            set
            {
                if (value == _totalGamePlayed) return;
                _totalGamePlayed = value;
                OnPropertyChanged();
            }
        }

        public ulong totalMainGamePlayed
        {
            get => _totalMainGamePlayed;
            set
            {
                if (value == _totalMainGamePlayed) return;
                ulong prevVal = _totalMainGamePlayed;
                _totalMainGamePlayed = value;
                totalGamePlayed += _totalMainGamePlayed - prevVal;
                OnPropertyChanged();
            }
        }

        public ulong totalFreeGamePlayed
        {
            get => _totalFreeGamePlayed;
            set
            {
                if (value == _totalFreeGamePlayed) return;
                _totalFreeGamePlayed = value;
                OnPropertyChanged();
            }
        }

        public ulong totalFgTriggered
        {
            get => _totalFgTriggered;
            set
            {
                if (value == _totalFgTriggered) return;
                _totalFgTriggered = value;
                OnPropertyChanged();
            }
        }

        public ulong totalFgRetriggered
        {
            get => _totalFgRetriggered;
            set
            {
                if (value == _totalFgRetriggered) return;
                _totalFgRetriggered = value;
                OnPropertyChanged();
            }
        }

        public ulong totalMgHit
        {
            get => _totalMgHit;
            set
            {
                if (_totalMgHit == value) return;
                _totalMgHit = value;
                OnPropertyChanged();
            }
        }

        public ulong totalFgHit
        {
            get => _totalFgHit;
            set
            {
                if (_totalFgHit == value) return;
                _totalFgHit = value;
                OnPropertyChanged();
            }
        }

        public string timeStart
        {
            get => _timeStart;
            set
            {
                if (timeStart == value) return;
                _timeStart = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Set to FREE_GAME in fg-init.
        /// Set to MAIN_GAME in fg-session-end.
        /// </summary>
        public byte currentSimDataIndex
        {
            get => _currentSimDataIndex;
           set
            {
                if (_currentSimDataIndex == value) return;
                _currentSimDataIndex = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potWinTotal
        {
            get => _potWinTotal;
            set
            {
                if (_potWinTotal == value) return;
                _potWinTotal = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potWinIcon
        {
            get => _potWinIcon;
            set
            {
                if (_potWinIcon == value) return;
                _potWinIcon = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potWinIngot
        {
            get => _potWinIngot;
            set
            {
                if (_potWinIngot == value) return;
                _potWinIngot = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potHit
        {
            get => _potHit;
            set
            {
                if (_potHit == value) return;
                _potHit = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potGamesPlayed
        {
            get => _potGamesPlayed;
            set
            {
                if (_potGamesPlayed == value) return;
                _potGamesPlayed = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potTriggered
        {
            get => _potTriggered;
            set
            {
                if (_potTriggered == value) return;
                _potTriggered = value;
                OnPropertyChanged();
            }
        }
        public ulong[] potRetriggered
        {
            get => _potRetriggered;
            set
            {
                if (_potRetriggered == value) return;
                _potRetriggered = value;
                OnPropertyChanged();
            }
        }

        public ulong[,] potJackpotHit
        {
            get => _potJackpotHit;
            set
            {
                if (_potJackpotHit == value) return;
                _potJackpotHit = value;
                OnPropertyChanged();
            }
        }
        public ulong[,] potJackpotWin
        {
            get => _potJackpotWin;
            set
            {
                if (_potJackpotWin == value) return;
                _potJackpotWin = value;
                OnPropertyChanged();
            }
        }

        // for dropdown purposes
        private string[] _variationNames;
        private string[] _denomNames =
        {
            "1",
            "2",
            "5",
            "10",
            "20",
            "50",
            "100",
            "200",
            "500",
            "1000",
            "2000",
        };

        private bool _simFgGameTypeIsRandom;
        private ushort _simFgGameTypeIndex;

        private bool _isSimulationStarted;
        private float _simElapsedTime;
        private ulong _totalTopUpAmount;
        private bool _isInFreeGame;

        private ulong _totalBet;
        private ulong _totalBetCount;

        //Win
        private ulong _totalWin;
        private ulong _totalGameWin;
        //Main Game
        private ulong _totalMgWin;// includes scatter win
        private ulong _totalScatterWin;
        private ulong _totalIconWin;
        private ulong _mgHighestWin;
        private ulong _singleMgWin;

        //Free Game
        private ulong _totalFgWin;
        private ulong _fgHighestWin;

        private ulong _totalGamePlayed;
        private ulong _totalMainGamePlayed;
        private ulong _totalFreeGamePlayed;
        private ulong _totalFgTriggered;
        private ulong _totalFgRetriggered;

        private ulong _totalMgHit;
        private ulong _totalFgHit;

        //Other
        private string _timeStart;

        // Free Game Specifics
        // Stored as [mg, B,R,G, BR, BG, RG, BRG]
        private const byte PotComboCount = 8;
        // Stored as [Grand, Major, Minor, Mini]
        private const byte FgJackpotCount = 4;

        /// <summary>
        /// Get index with SimGM.ConvertPotFeatureFlagToSimDataIndex()
        /// </summary>
        private byte _currentSimDataIndex;

        private ulong[] _potWinTotal = new ulong[PotComboCount];
        private ulong[] _potWinIcon = new ulong[PotComboCount];
        private ulong[] _potWinIngot = new ulong[PotComboCount];
        private ulong[] _potHit = new ulong[PotComboCount];
        private ulong[] _potGamesPlayed = new ulong[PotComboCount];
        private ulong[] _potTriggered = new ulong[PotComboCount];
        private ulong[] _potRetriggered = new ulong[PotComboCount];

        /// <summary>
        /// FreeGame Jackpot Hits [MainGame, B,R,G, BR, BG, RG, BRG] x [Grand, Major, Minor, Mini]
        /// </summary>
        private ulong[,] _potJackpotHit = new ulong[PotComboCount, FgJackpotCount];
        private ulong[,] _potJackpotWin = new ulong[PotComboCount, FgJackpotCount];
    }
}