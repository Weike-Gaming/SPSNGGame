using System;
using System.ComponentModel;
using System.Reflection;
using TMPro;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYSimGameCountDisplay : WkSimDisplayObject
    {
        [SerializeField] private TextMeshProUGUI mgText;
        [SerializeField] private TextMeshProUGUI elapsedTime;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYSimulationGameManager gm = owningPlayerController?.owner as JIXRYSimulationGameManager ?? throw new InvalidCastException();

            void OnPropChanges(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName.Equals("simElapsedTime"))
                {
                    PropertyInfo elapseInfo = sender.GetType().GetProperty(e.PropertyName);
                    float time = (float)elapseInfo!.GetValue(sender);
                    elapsedTime.text = time.ToString();
                    return;
                }

                if (e.PropertyName.Equals("totalMainGamePlayed"))
                {
                    PropertyInfo info = sender.GetType().GetProperty(e.PropertyName);
                    ulong obj = (ulong)info!.GetValue(sender);
                    mgText.text = obj.ToString();
                    return;
                }
            }

            gm.dataModel.PropertyChanged += (sender, args) => { OnPropChanges(sender, args); };
        }

        protected override void OnAllowedEnable()
        {
            mgText.enabled = true;
            elapsedTime.enabled = true;
        }

        protected override void ResetToDefault()
        {

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            mgText.enabled = false;
            elapsedTime.enabled = false;
        }
    }
}
