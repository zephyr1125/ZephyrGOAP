using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class FacilityProgressManager : MonoBehaviour
    {
        private static FacilityProgressManager _instance;

        public static FacilityProgressManager Instance => _instance;
        
        public GameObject FacilityProgressGameObject;

        private Dictionary<Entity, FacilityProgress> _facilities;
        
        private void Awake()
        {
            _instance = this;
            _facilities = new Dictionary<Entity, FacilityProgress>();
        }
        
        public void UpdateFacilityProgress(Entity facilityEntity, float progress, float3 position)
        {
            var info = 
                !_facilities.ContainsKey(facilityEntity) ? AddFacilityProgress(facilityEntity) : _facilities[facilityEntity];
            info.SetProgress(progress, position);
        }
        
        private FacilityProgress AddFacilityProgress(Entity agentEntity)
        {
            var go = Instantiate(FacilityProgressGameObject, transform);
            go.SetActive(true);
            var facilityProgress = go.GetComponent<FacilityProgress>();
            _facilities.Add(agentEntity, facilityProgress);
            return facilityProgress;
        }
    }
}