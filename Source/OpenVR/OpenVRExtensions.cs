using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

namespace FlaxVR.OpenVR
{
    internal static class OpenVRExtensions
    {
        /// <summary>
        /// Gets whether the button is pressed.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="buttonId">The button identifier.</param>
        /// <returns></returns>
        internal static bool GetButtonPressed(this VRControllerState_t state, EVRButtonId buttonId) => (state.ulButtonPressed & 1UL << (int)buttonId) != 0UL;

        /// <summary>
        /// Gets whether the button is touched.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="buttonId">The button identifier.</param>
        /// <returns></returns>
        internal static bool GetButtonTouched(this VRControllerState_t state, EVRButtonId buttonId) => (state.ulButtonTouched & 1UL << (int)buttonId) != 0UL;

        /// <summary>
        /// Converts <see cref="Boolean"/> to <see cref="VRButtonState"/>
        /// </summary>
        /// <param name="b">The input.</param>
        /// <returns>VRButtonState corresponding to input <paramref name="b"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static VRButtonState ToButtonState(this bool b)
        {
            if (!b)
                return VRButtonState.Released;
            return VRButtonState.Pressed;
        }
    }
}
