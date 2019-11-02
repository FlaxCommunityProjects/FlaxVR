using FlaxEngine.Rendering;
using FlaxVR.OpenVR;
using System;
using System.Collections.Generic;

namespace FlaxVR
{
    public abstract class VRContext : IDisposable
    {
        internal VRContext() { }

        public abstract void Initialize();

        public abstract string DeviceName { get; }

        public abstract RenderTarget LeftEyeRenderTarget { get; }
        public abstract RenderTarget RightEyeRenderTarget { get; }

        public abstract List<VRControllerState> Controllers { get; }

        public abstract int LeftControllerIndex { get; }

        public abstract int RightControllerIndex { get; }

        public abstract void UpdateProjectionMatrices(float zNear, float zFar);
        public abstract void UpdateDevices();

        public abstract HmdPoseState WaitForPoses();
        public abstract void SubmitFrame();
        public abstract void Dispose();

        // TODO: Implement Oculus (Required: Get oculus ;D)
        /*public static VRContext CreateOculus() => CreateOculus(default);
        public static VRContext CreateOculus() => new OculusContext();
        public static bool IsOculusSupported() => OculusContext.IsSupported();*/

        public static VRContext CreateOpenVR() => CreateOpenVR(new VRContextOptions()); // TODO: default when C# 7.1
        public static VRContext CreateOpenVR(VRContextOptions options) => new OpenVRContext(options);
        public static bool IsOpenVRSupported() => OpenVRContext.IsSupported();
    }
}
