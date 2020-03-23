using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class Utils
    {
        public static void AddStatesToContainer(VisualElement container, StateView[] states)
        {
            if (states == null) return;
            
            var stateTexts = new List<string>(states.Length);
            for (var i = 0; i < states.Length; i++)
            {
                stateTexts.Add(states[i].ToString());
            }
            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) => ((Label) e).text = stateTexts[i];
            var list = new ListView(stateTexts, 16, makeItem, bindItem);
            list.selectionType = SelectionType.None;
            list.style.flexGrow = 1;
            
            container.Add(list);
        }
    }
}