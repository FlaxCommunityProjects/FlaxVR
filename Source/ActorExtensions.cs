using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public static class ActorExtensions
    {
        public static List<T> GetScriptsRecursive<T>(this Actor actor, List<T> currentList = null) where T: Script
        {
            if (currentList == null)
                currentList = new List<T>();

            currentList.AddRange(actor.GetScripts<T>());

            for (int i = 0; i < actor.ChildrenCount; i++)
                currentList.AddRange(actor.GetChild(i).GetScriptsRecursive<T>(currentList));

            return currentList;
        }
    }
}
