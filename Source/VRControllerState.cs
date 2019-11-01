using Valve.VR;

namespace FlaxVR
{
    /// <summary>
    /// State of OpenVR Controller
    /// </summary>
    public class VRControllerState
    {
        /// <summary>
        /// Gets or sets the pose.
        /// </summary>
        /// <value>
        /// The pose.
        /// </value>
        public VRPose Pose { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets the controller role
        /// </summary>
        public VRControllerRole Role;

        /// <summary>
        /// Direction Pad buttons on the controller
        /// </summary>
        public VRGamepadDPad DPad;

        /// <summary>
        /// Grip button on the controller
        /// </summary>
        public VRButtonState Grip;

        /// <summary>
        /// Menu button on the controller
        /// </summary>
        public VRButtonState ApplicationMenu;

        /// <summary>
        /// Button A on the controller
        /// </summary>
        public VRButtonState A;

        /// <summary>
        /// Trackpad button on the controller
        /// </summary>
        public VRButtonState TrackpadButton;

        /// <summary>
        /// Trigger button on the controller
        /// </summary>
        public VRButtonState TriggerButton;

        /// <summary>
        /// Gets or sets a value indicating whether the trackpad is touched.
        /// </summary>
        public bool TrackpadTouch;

        /// <summary>
        /// Trigger value, in the range 0.0 to 1.0f.
        /// </summary>
        public float Trigger;

        /// <summary>
        /// Trackpad axis values, in the range -1.0f to 1.0f.
        /// </summary>
        public Vector2 Trackpad;

        /// <summary>
        /// Updates the controller.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="state">The state.</param>
        /// <param name="pose">The pose.</param>
        internal void Update(VRControllerRole role, ref VRControllerState_t state, ref VRPose pose)
        {
            IsConnected = true;
            Pose = pose;
            Role = role;

            // Trackpad
            Trackpad.X = state.rAxis0.x;
            Trackpad.Y = state.rAxis0.y;
            TrackpadTouch = state.GetButtonTouched(EVRButtonId.k_EButton_Axis0);
            TrackpadButton = state.GetButtonPressed(EVRButtonId.k_EButton_Axis0).ToButtonState();

            // Trigger
            Trigger = state.rAxis1.x;
            TriggerButton = state.GetButtonPressed(EVRButtonId.k_EButton_Axis1).ToButtonState();

            // Buttons
            ApplicationMenu = state.GetButtonPressed(EVRButtonId.k_EButton_ApplicationMenu).ToButtonState();
            A = state.GetButtonPressed(EVRButtonId.k_EButton_A).ToButtonState();
            Grip = state.GetButtonPressed(EVRButtonId.k_EButton_Grip).ToButtonState();

            // DPad
            DPad.Up = state.GetButtonPressed(EVRButtonId.k_EButton_DPad_Up).ToButtonState();
            DPad.Down = state.GetButtonPressed(EVRButtonId.k_EButton_DPad_Down).ToButtonState();
            DPad.Left = state.GetButtonPressed(EVRButtonId.k_EButton_DPad_Left).ToButtonState();
            DPad.Right = state.GetButtonPressed(EVRButtonId.k_EButton_DPad_Right).ToButtonState();
        }
    }
}