using System;
using FlaxEngine;
using System.Text;
using Valve.VR;
using OVR = Valve.VR.OpenVR;
using FlaxEngine.Rendering;
using System.Collections.Generic;

namespace FlaxVR.OpenVR
{
    internal class OpenVRContext : VRContext
    {
        private readonly CVRSystem _vrSystem;
        private readonly CVRCompositor _compositor;
        private readonly VRContextOptions _options;
        private string _deviceName;
        private RenderTarget _leftEyeRT;
        private RenderTarget _rightEyeRT;
        private Matrix _projLeft;
        private Matrix _projRight;
        private Matrix _headToEyeLeft;
        private Matrix _headToEyeRight;
        private TrackedDevicePose_t[] _devicePoses = new TrackedDevicePose_t[OVR.k_unMaxTrackedDeviceCount];
        private readonly List<VRTrackingReference> _trackingReferences = new List<VRTrackingReference>();
        private readonly List<VRControllerState> _controllers = new List<VRControllerState>();

        public override RenderTarget LeftEyeRenderTarget => _leftEyeRT;
        public override RenderTarget RightEyeRenderTarget => _rightEyeRT;

        public override string DeviceName => _deviceName;

        /// <summary>
        /// Gets the index of the left controller.
        /// </summary>
        /// <value>
        /// The index of the left controller.
        /// </value>
        public int LeftControllerIndex { get; private set; }

        /// <summary>
        /// Gets the index of the right controller.
        /// </summary>
        /// <value>
        /// The index of the right controller.
        /// </value>
        public int RightControllerIndex { get; private set; }

        /// <summary>
        /// Gets the left controller.
        /// </summary>
        /// <value>
        /// The left controller.
        /// </value>
        public VRControllerState LeftController
        {
            get
            {
                if (LeftControllerIndex >= 0 && LeftControllerIndex < _controllers.Count)
                    return _controllers[LeftControllerIndex];
                return null;
            }
        }

        /// <summary>
        /// Gets the right controller.
        /// </summary>
        /// <value>
        /// The right controller.
        /// </value>
        public VRControllerState RightController
        {
            get
            {
                if (RightControllerIndex >= 0 && RightControllerIndex < _controllers.Count)
                {
                    return _controllers[RightControllerIndex];
                }
                return null;
            }
        }

        public OpenVRContext(VRContextOptions options)
        {
            _options = options;

            EVRInitError initError = EVRInitError.None;
            _vrSystem = OVR.Init(ref initError, EVRApplicationType.VRApplication_Scene);
            if (initError != EVRInitError.None)
                throw new Exception($"Failed to initialize OpenVR: {OVR.GetStringForHmdError(initError)}");

            _compositor = OVR.Compositor;
            if (_compositor == null)
                throw new Exception("Failed to access the OpenVR Compositor.");
        }

        internal static bool IsSupported()
        {
            try
            {
                return OVR.IsHmdPresent();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the devices.
        /// </summary>
        private void UpdateDevices()
        {
            // Get indexes of left and right controllers
            LeftControllerIndex = -1;
            RightControllerIndex = -1;
            int lhIndex = (int)_vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            int rhIndex = (int)_vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

            _controllers.Clear();
            _trackingReferences.Clear();

            // Update all tracked devices
            for (uint i = 0; i < OVR.k_unMaxTrackedDeviceCount; i++)
            {
                TrackedDevicePose_t pose = _devicePoses[i];

                // We are interested in valid and connected devices only
                if (pose.bDeviceIsConnected && pose.bPoseIsValid)
                {
                    ToVRPose(pose, out Vector3 posePosition, out Quaternion poseOrientation);
                    ETrackedDeviceClass c = _vrSystem.GetTrackedDeviceClass(i);

                    // Update controller
                    if (c == ETrackedDeviceClass.Controller)
                    {
                        VRControllerState_t state_t = default(VRControllerState_t);
                        if (_vrSystem.GetControllerState(i, ref state_t, (uint)System.Runtime.InteropServices.Marshal.SizeOf(state_t)))
                        {
                            VRControllerRole role;
                            if (i == lhIndex)
                            {
                                role = VRControllerRole.LeftHand;
                                LeftControllerIndex = _controllers.Count;
                            }
                            else if (i == rhIndex)
                            {
                                role = VRControllerRole.RightHand;
                                RightControllerIndex = _controllers.Count;
                            }
                            else
                            {
                                role = VRControllerRole.Undefined;
                            }

                            VRControllerState state = new VRControllerState();
                            state.Update(role, ref state_t, posePosition, poseOrientation);
                            _controllers.Add(state);
                        }
                    }
                    // Update generic reference (base station etc...)
                    else if (c == ETrackedDeviceClass.TrackingReference)
                    {
                        VRTrackingReference reference = new VRTrackingReference();
                        reference.Update(posePosition, poseOrientation);
                        _trackingReferences.Add(reference);
                    }
                }
            }
        }


        public override void Dispose()
        {
            _leftEyeRT.Dispose();
            _rightEyeRT.Dispose();
            OVR.Shutdown();
        }

        public override void Initialize()
        {
            StringBuilder sb = new StringBuilder(512);
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            uint ret = _vrSystem.GetStringTrackedDeviceProperty(
                OVR.k_unTrackedDeviceIndex_Hmd,
                ETrackedDeviceProperty.Prop_TrackingSystemName_String,
                sb,
                512u,
                ref error);
            if (error != ETrackedPropertyError.TrackedProp_Success)
                _deviceName = "<Unknown OpenVR Device>";
            else
                _deviceName = sb.ToString();

            uint eyeWidth = 0;
            uint eyeHeight = 0;
            _vrSystem.GetRecommendedRenderTargetSize(ref eyeWidth, ref eyeHeight);

            _leftEyeRT = RenderTarget.New();
            _leftEyeRT.Init(PixelFormat.R8G8B8A8_UNorm, (int)eyeWidth, (int)eyeHeight, multiSampleLevel: _options.EyeRenderTargetSampleCount);

            _rightEyeRT = RenderTarget.New();
            _rightEyeRT.Init(PixelFormat.R8G8B8A8_UNorm, (int)eyeWidth, (int)eyeHeight, multiSampleLevel: _options.EyeRenderTargetSampleCount);

            Matrix eyeToHeadLeft = ToSysMatrix(_vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left));
            Matrix.Invert(ref eyeToHeadLeft, out _headToEyeLeft);

            Matrix eyeToHeadRight = ToSysMatrix(_vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right));
            Matrix.Invert(ref eyeToHeadRight, out _headToEyeRight);

            // Default RH matrices
            /*_projLeft = ToSysMatrix(_vrSystem.GetProjectionMatrix(EVREye.Eye_Left, 0.1f, 10000f));
            _projRight = ToSysMatrix(_vrSystem.GetProjectionMatrix(EVREye.Eye_Right, 0.1f, 10000f));*/

            // Build LH projection matrices (https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw)
            float pLeft = 0;
            float pRight = 0;
            float pTop = 0;
            float pBottom = 0;

            // Default values
            float zNear = 0.1f;
            float zFar = 20000f;

            _vrSystem.GetProjectionRaw(EVREye.Eye_Left, ref pLeft, ref pRight, ref pTop, ref pBottom);
            _projLeft = Matrix.PerspectiveOffCenter(pLeft * zNear, pRight * zNear, pBottom * zNear * -1f, pTop * zNear * -1f, zNear, zFar);

            _vrSystem.GetProjectionRaw(EVREye.Eye_Right, ref pLeft, ref pRight, ref pTop, ref pBottom);
            _projRight = Matrix.PerspectiveOffCenter(pLeft * zNear, pRight * zNear, pBottom * zNear * -1f, pTop * zNear * -1f, zNear, zFar);
        }

        public override void UpdateProjectionMatrices(float zNear, float zFar)
        {
            // Default RH matrices
            /*_projLeft = ToSysMatrix(_vrSystem.GetProjectionMatrix(EVREye.Eye_Left, 0.1f, 10000f));
            _projRight = ToSysMatrix(_vrSystem.GetProjectionMatrix(EVREye.Eye_Right, 0.1f, 10000f));*/

            // Build LH projection matrices (https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw)
            float pLeft = 0;
            float pRight = 0;
            float pTop = 0;
            float pBottom = 0;

            _vrSystem.GetProjectionRaw(EVREye.Eye_Left, ref pLeft, ref pRight, ref pTop, ref pBottom);
            _projLeft = Matrix.PerspectiveOffCenter(pLeft * zNear, pRight * zNear, pBottom * zNear * -1f, pTop * zNear * -1f, zNear, zFar);

            _vrSystem.GetProjectionRaw(EVREye.Eye_Right, ref pLeft, ref pRight, ref pTop, ref pBottom);
            _projRight = Matrix.PerspectiveOffCenter(pLeft * zNear, pRight * zNear, pBottom * zNear * -1f, pTop * zNear * -1f, zNear, zFar);
        }

        public override void SubmitFrame()
        {
            SubmitTexture(_compositor, LeftEyeRenderTarget, EVREye.Eye_Left);
            SubmitTexture(_compositor, RightEyeRenderTarget, EVREye.Eye_Right);
        }
        private void SubmitTexture(CVRCompositor compositor, RenderTarget colorTex, EVREye eye)
        {
            Texture_t texT;

            var renderer = GraphicsDevice.RendererType;

            if (renderer == RendererType.DirectX10 || renderer == RendererType.DirectX10_1 || renderer == RendererType.DirectX11)
            {
                texT.handle = colorTex.NativePtr;
                texT.eColorSpace = EColorSpace.Gamma;
                texT.eType = ETextureType.DirectX;
            }
            else
            {
                throw new Exception($"Renderer '{renderer}' is not yet supported");
            }

            /*if (rt == RendererType.DirectX12)
            {
                texT.handle = colorTex.NativePtr;
                texT.eColorSpace = EColorSpace.Gamma;
                texT.eType = ETextureType.DirectX12;
            }

            if(rt == RendererType.Vulkan)
            {
                texT.handle = colorTex.NativePtr;
                texT.eColorSpace = EColorSpace.Gamma;
                texT.eType = ETextureType.Vulkan;
            }*/


            VRTextureBounds_t boundsT;
            boundsT.uMin = 0;
            boundsT.uMax = 1;
            boundsT.vMin = 0;
            boundsT.vMax = 1;

            EVRCompositorError compositorError = EVRCompositorError.None;
            compositorError = compositor.Submit(eye, ref texT, ref boundsT, EVRSubmitFlags.Submit_Default);

            if (compositorError != EVRCompositorError.None)
                throw new Exception($"Failed to submit to the OpenVR Compositor: {compositorError}");
        }
        public override HmdPoseState WaitForPoses()
        {
            EVRCompositorError compositorError = _compositor.WaitGetPoses(_devicePoses, new TrackedDevicePose_t[0]);

            TrackedDevicePose_t hmdPose = _devicePoses[OVR.k_unTrackedDeviceIndex_Hmd];
            Matrix deviceToAbsolute = ToSysMatrix(hmdPose.mDeviceToAbsoluteTracking);
            deviceToAbsolute.Decompose(out _, out Quaternion deviceRotation, out Vector3 devicePosition);
            Matrix.Invert(ref deviceToAbsolute, out Matrix absoluteToDevice);

            Matrix viewLeft = absoluteToDevice * _headToEyeLeft;
            Matrix viewRight = absoluteToDevice * _headToEyeRight;

            Matrix.Invert(ref viewLeft, out Matrix invViewLeft);
            invViewLeft.Decompose(out Vector3 scale, out Quaternion leftRotation, out Vector3 leftPosition);

            Matrix.Invert(ref viewRight, out Matrix invViewRight);
            invViewRight.Decompose(out _, out Quaternion rightRotation, out Vector3 rightPosition);

            // HMD (meters) vs Flax (centimeters) scaling
            leftPosition *= 100;
            rightPosition *= 100;

            // TODO: Expose mDeviceToAbsoluteTracking as "Head position/rotation" so we can move master actor properly
            return new HmdPoseState(
                devicePosition, deviceRotation,
                _projLeft, _projRight,
                leftPosition, rightPosition,
                leftRotation, rightRotation);
        }

        private static Matrix ToSysMatrix(HmdMatrix34_t hmdMat)
        {
            // Includes RH to LH conversion
            return new Matrix(
                 hmdMat.m0, hmdMat.m4, -hmdMat.m8, 0f,
                 hmdMat.m1, hmdMat.m5, -hmdMat.m9, 0f,
                -hmdMat.m2, -hmdMat.m6, hmdMat.m10, 0f,
                 hmdMat.m3, hmdMat.m7, -hmdMat.m11, 1f);
        }

        private static Matrix ToSysMatrix(HmdMatrix44_t hmdMat)
        {
            // DOESN'T include RH to LH conversion (since projection matrices are now built from raw data instead)
            return new Matrix(
                hmdMat.m0, hmdMat.m4, hmdMat.m8, hmdMat.m12,
                hmdMat.m1, hmdMat.m5, hmdMat.m9, hmdMat.m13,
                hmdMat.m2, hmdMat.m6, hmdMat.m10, hmdMat.m14,
                hmdMat.m3, hmdMat.m7, hmdMat.m11, hmdMat.m15);
        }

        public static void ToVRPose(TrackedDevicePose_t trackedPose, out Vector3 position, out Quaternion orientation)
        {
            Matrix matrix = ToSysMatrix(trackedPose.mDeviceToAbsoluteTracking);
            position = matrix.TranslationVector * 100; //m -> cm
            orientation = Quaternion.RotationMatrix(matrix);
        }
    }
}
