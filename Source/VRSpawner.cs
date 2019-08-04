using FlaxEngine;

namespace FlaxVR.Source
{
	public class VRSpawner : Script
	{
		private void Update()
		{
            // For testing only... could probably be in scene directly
            if (Input.GetKeyUp(Keys.W))
                Actor.AddScript<VRCamera>();

            if (Input.GetKeyUp(Keys.S))
                Actor.RemoveScript(Actor.GetScript<VRCamera>());
		}
	}
}
