using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.GOAP.Sample.Game.Component;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class ItemContainer : MonoBehaviour
    {
        public Text text;

        private Transform _transform;

        private void OnEnable()
        {
            _transform = transform;
        }

        public void SetItems(NativeList<FixedString32> itemNames, NativeList<byte> itemCounts, Vector3 position)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < itemNames.Length; i++)
            {
                var itemName = itemNames[i];
                var itemAmount = itemCounts[i];
                stringBuilder.Append($"{itemName} * {itemAmount}\n");
            }

            text.text = stringBuilder.ToString();
            
            _transform.position = position;
        }
    }
}