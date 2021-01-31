using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;

namespace Utils.Torch
{
    [XmlType("QuestLogItem")]
    public struct Quest : IDisposable
    {
        private string _titile;
        private string _description;
        private long _playerId;
        private TimeSpan _disposeTime;
        
        [XmlIgnore]
        internal Timer DisposeTimer;

        [XmlElement("Titile")]
        internal string TitileImpl { get => _titile; set => _titile = value; }

        [XmlElement("Description")]
        internal string DescriptionImpl { get => _description; set => _description = value; }

        [XmlElement("PlayerId")]
        internal long PlayerIdImpl { get => _playerId; set => _playerId = value; }

        [XmlIgnore]
        public string Titile
        {
            get => _titile; set
            {
                _titile = value;
                MyVisualScriptLogicProvider.SetQuestlogTitle(_titile, _playerId);
            }
        }

        [XmlIgnore]
        public string Description
        {
            get => _description; set
            {
                if (!string.IsNullOrEmpty(_description))
                    MyVisualScriptLogicProvider.ReplaceQuestlogDetail(newDetail: value, playerId: _playerId);
                else
                {
                    MyVisualScriptLogicProvider.AddQuestlogDetail(value, playerId: _playerId);
                    var playerId = _playerId;
                    ObjectivesList.ForEach(b => MyVisualScriptLogicProvider.AddQuestlogObjective(b.Key, playerId: playerId));
                }
                _description = value;
            }
        }

        [XmlArray("Objectives"), XmlArrayItem("Objective")]
        internal List<KeyValuePair<string, bool>> ObjectivesList { get; set; }

        [XmlIgnore]
        public IReadOnlyList<KeyValuePair<string, bool>> Objectives => ObjectivesList;

        [XmlIgnore]
        public long PlayerId
        {
            get => _playerId; set
            {
                if (_playerId != default)
                {
                    MyVisualScriptLogicProvider.SetQuestlog(false, playerId: _playerId);
                }
                _playerId = value;

                if (string.IsNullOrEmpty(_titile))
                    return;

                MyVisualScriptLogicProvider.SetQuestlog(questName: _titile, playerId: _playerId);
                MyVisualScriptLogicProvider.AddQuestlogDetail(_description, playerId: _playerId);
                var playerId = _playerId;
                ObjectivesList.ForEach(b => MyVisualScriptLogicProvider.AddQuestlogObjective(b.Key, playerId: playerId));
            }
        }

        [XmlIgnore]
        public TimeSpan DisposeTime { get => _disposeTime; set
            {
                if (value == default)
                    throw new ArgumentNullException(nameof(value));
                _disposeTime = value;

                if (DisposeTimer == default)
                    DisposeTimer = new Timer(b => ((Quest)b).Dispose(), this, _disposeTime, Timeout.InfiniteTimeSpan);
                else
                    DisposeTimer.Change(_disposeTime, Timeout.InfiniteTimeSpan);
            }
        }

        public Quest(string title, long playerId)
        {
            if (playerId == default || string.IsNullOrEmpty(title))
                throw new ArgumentOutOfRangeException();

            ObjectivesList = new List<KeyValuePair<string, bool>>();
            _titile = title;
            _description = string.Empty;
            _playerId = playerId;
            _disposeTime = default;
            DisposeTimer = default;
            MyVisualScriptLogicProvider.SetQuestlog(questName: _titile, playerId: _playerId);
        }

        public void SetObjectiveStatus(int index, bool status)
        {
            var num = index - 1;
            if (num < 0 || num > ObjectivesList.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(index));

            MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(num, status, _playerId);
        }
        public void SetObjectiveStatus(KeyValuePair<string, bool> objective, bool status) => SetObjectiveStatus(ObjectivesList.IndexOf(objective), status);

        public void AddObjective(KeyValuePair<string, bool> item, bool completePrevious = false, bool useTyping = true)
        {
            MyVisualScriptLogicProvider.AddQuestlogObjective(item.Key, completePrevious, useTyping, _playerId);
        }

        public void Dispose()
        {
            if (_playerId != default)
                MyVisualScriptLogicProvider.SetQuestlog(false, playerId: _playerId);
            DisposeTimer?.Dispose();
        }
    }
}
