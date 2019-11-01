using System;
using System.Collections.Generic;
using Classic.AI.GOAP;

namespace Classic
{
    public class Utils
    {
        public static string PrettyPrint(Dictionary<string,object> state) {
            var s = "";
            foreach (var item in state) {
                s += item.Key + ":" + item.Value;
                s += ", ";
            }
            return s;
        }

        public static string PrettyPrint(Queue<GoapAction> actions) {
            var s = "";
            foreach (GoapAction a in actions) {
                s += a.GetType().Name;
                s += "-> ";
            }
            s += "GOAL";
            return s;
        }

        public static string PrettyPrint(GoapAction[] actions) {
            var s = "";
            foreach (GoapAction a in actions) {
                s += a.GetType().Name;
                s += ", ";
            }
            return s;
        }

        public static string PrettyPrint(GoapAction action) {
            var s = ""+action.GetType().Name;
            return s;
        }
    }
}