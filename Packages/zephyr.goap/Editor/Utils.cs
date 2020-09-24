using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class Utils
    {
        public const string WindowFilePath = "Packages/zephyr.goap/Editor/UXML/window.uxml";
        public const string NodeFilePath = "Packages/zephyr.goap/Editor/UXML/node.uxml";
        public const string StateFilePath = "Packages/zephyr.goap/Editor/UXML/states.uxml";
        public const string TimeLineNodeFilePath = "Packages/zephyr.goap/Editor/UXML/timeline_node.uxml";
        
        public const float DoubleClickThreshold = 0.2f;
        
        private static Dictionary<Entity, StyleColor> _agentColors;
        private static readonly Color BaseAgentColor = new Color(0f, 0.29f, 0.12f);
        
        public static void AddStatesToContainer(VisualElement container, StateLog[] states)
        {
            if (states == null) return;
            
            var sorted = new SortedSet<StateLog>(states);
            var stateTexts = new List<string>(states.Length);
            foreach (var state in sorted)
            {
                stateTexts.Add(state.ToString());
            }
            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) => ((Label) e).text = stateTexts[i];
            var list = new ListView(stateTexts, 16, makeItem, bindItem);
            list.selectionType = SelectionType.None;
            list.style.flexGrow = 1;
            
            container.Add(list);
        }
        
        public static StyleColor GetAgentColor(EntityLog agentEntity)
        {
            var agentEntityStruct = new Entity{Index = agentEntity.index, Version = agentEntity.version};
            if(_agentColors == null)_agentColors = new Dictionary<Entity, StyleColor>();

            if (!_agentColors.ContainsKey(agentEntityStruct))
            {
                var agentSum = _agentColors.Count;
                Color.RGBToHSV(BaseAgentColor, out var h, out var s, out var v);
                var newH = h + 0.11f * agentSum;
                var color = Color.HSVToRGB(newH - (int) newH, s, v);
                
                _agentColors.Add(agentEntityStruct, color);
            }

            return _agentColors[agentEntityStruct];
        }
    }
}