//using DOTS.Component;
//using Unity.Entities;
//using Unity.Jobs;
//using Zephyr.GOAP.Runtime.Component;
//
//namespace DOTS.System
//{
//    public class ExpandSystem : JobComponentSystem
//    {    
//        /// <summary>
//        /// 广度优先建立起用于寻路的node graph
//        /// </summary>
//        private struct ExpandJob : IJobForEach<Goal, GoapAgent>
//        {
//            public void Execute(ref Goal goal, ref GoapAgent agent)
//            {
//                //建立可以执行的action列表
//                
//                //由goal产生node
//                //遍历action表并找到可以满足goal的action
//                    //根据环境现状产生多个同一action的setting
//                    //每套setting产生一个新的leaf node
//                    //在node中存入新对比产生的goal
//                //回到开头扩展循环或者递归
//            }
//        }
//        
//        protected override JobHandle OnUpdate(JobHandle inputDeps)
//        {
//            
//        }
//    }
//}