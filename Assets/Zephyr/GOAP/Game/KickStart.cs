using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
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
                var agentAEntity = Entity.Null;
                foreach (var entity in entities)
                {
                    if (entityManager.HasComponent<CookerTrait>(entity))
                    {
                        cookerEntity = entity;
                    }else if (entityManager.HasComponent<Agent>(entity))
                    {
                        agentAEntity = entity;
                    }

                }
                entities.Dispose();
                
                var goalState = new State
                {
                    // Target = cookerEntity,
                    // Position = entityManager.GetComponentData<Translation>(cookerEntity).Value,
                    // Trait = typeof(ItemSourceTrait),
                    // ValueString = "feast"
                    Target = agentAEntity,
                    Trait = typeof(StaminaTrait),
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