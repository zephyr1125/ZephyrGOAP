using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Game
{
    public class KickStart : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.N))
            {
                var world = World.AllWorlds[0];
                var entityManager = world.EntityManager;
                var goalEntity = entityManager.CreateEntity();
                var entities = entityManager.GetAllEntities();
                var cookerEntity = Entity.Null;
                foreach (var entity in entities)
                {
                    if (!entityManager.HasComponent<CookerTrait>(entity)) continue;
                    cookerEntity = entity;
                    break;

                }
                entities.Dispose();
                
                var goalState = new State
                {
                    Target = cookerEntity,
                    Trait = typeof(ItemSourceTrait),
                    ValueString = "roast_apple"
                };
                entityManager.AddComponentData(goalEntity, new Goal
                {
                    GoalEntity = goalEntity,
                    State = goalState,
                    Priority = Priority.Normal,
                    CreateTime = 0
                });
                entityManager.AddComponentData(goalEntity, new IdleGoal
                {
                    Time = (float)0
                });
            }
        }
    }
}