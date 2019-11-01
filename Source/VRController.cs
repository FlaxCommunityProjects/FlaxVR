using FlaxEngine;
using FlaxEngine.VR;

namespace UI
{
    public class VRController : Script
    {
        public VRControllerRole Role { get; set; }

        public int ControllerIndex { get; set; }

        [HideInEditor]
        public VRControllerState State { get; internal set; }

        [HideInEditor]
        public bool IsConnected => State.IsConnected;

        internal void UpdateState(VRControllerState newState)
        {
            State = newState;
            if (State.IsConnected)
            {
                var transform = LocalTransform;

                transform.Translation = State.Pose.Position;
                transform.Orientation = State.Pose.Orientation;

                LocalTransform = transform;
            }
        }
    }
}