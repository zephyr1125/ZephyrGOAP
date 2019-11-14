//using DOTS.ActionJob;
//using DOTS.Component;
//using DOTS.Component.Actions;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Zephyr.GOAP.Runtime.Component;
//
//namespace DOTS.System
//{
//    [UpdateInGroup(typeof(InitializationSystemGroup))]
//    public class ExpandSystem : JobComponentSystem
//    {
//        /// <summary>
//        /// todo for debug
//        /// </summary>
//        public StateGroup GoalStates;
//        /// <summary>
//        /// todo for debug
//        /// </summary>
//        public StackData StackData;
//        
//        private EntityQuery _agentQuery;
//        
//        private EndInitializationEntityCommandBufferSystem _ecbSystem;
//
//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            _agentQuery = GetEntityQuery(
//                ComponentType.ReadOnly<Agent>(),
//                ComponentType.ReadOnly<GoalPlanning>(),
//                ComponentType.ReadOnly<Action>());
//            _ecbSystem =
//                World.Active.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
//        }
//        
//        public NativeList<Agent> UnexpandedStates { get; set; }
//
//        protected override JobHandle OnUpdate(JobHandle inputDeps)
//        {
//            //主循环遍历赋予了goal的空闲agent
//                //遍历其可以执行的action，把对应job名存入容器
//                //从goal开始对适用的action进行expand
//
//            var agentEntities = _agentQuery.ToEntityArray(Allocator.TempJob);
//            foreach (var agentEntity in agentEntities)
//            {
//                var bufferAction = GetBufferFromEntity<Action>(true);
//                foreach (var action in bufferAction[agentEntity])
//                {
//                    var actionJob = ActionScheduler.Instance().GetJob(action.ActionJobName.ToString());
//                    ((IActionJob) actionJob).GoalStates = GoalStates;
//                    ((IActionJob) actionJob).StackData = StackData;
//                    ((IActionJob)actionJob).ECBuffer = _ecbSystem.CreateCommandBuffer().ToConcurrent();
//                    var jobHandle = actionJob.Schedule(UnexpandedStates, 0, inputDeps);
//                    _ecbSystem.AddJobHandleForProducer(jobHandle);
//                }
//            }
//            
//            return inputDeps;
//        }
//    }
//}