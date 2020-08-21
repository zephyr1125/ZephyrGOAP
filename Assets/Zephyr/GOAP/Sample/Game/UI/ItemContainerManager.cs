using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Zephyr.GOAP.Sample.Game.Component;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class ItemContainerManager : MonoBehaviour
    {
        private static ItemContainerManager _instance;

        public static ItemContainerManager Instance => _instance;
        
        public GameObject ItemContainerGameObject;

        private Dictionary<Entity, ItemContainer> _itemContainers;

        private Transform _transform;
        
        private void Awake()
        {
            _instance = this;
            _transform = transform;
            _itemContainers = new Dictionary<Entity, ItemContainer>();
        }
        
        public void UpdateItemContainer(Entity itemContainerEntity,
            NativeList<FixedString32> itemNames, NativeList<byte> itemCounts, float3 position)
        {
            var itemContainer = 
                !_itemContainers.ContainsKey(itemContainerEntity) ? AddItemContainer(itemContainerEntity) : _itemContainers[itemContainerEntity];
            itemContainer.SetItems(itemNames, itemCounts, position);
        }
        
        private ItemContainer AddItemContainer(Entity agentEntity)
        {
            var go = Instantiate(ItemContainerGameObject, _transform);
            go.SetActive(true);
            var itemContainer = go.GetComponent<ItemContainer>();
            _itemContainers.Add(agentEntity, itemContainer);
            return itemContainer;
        }
    }
}