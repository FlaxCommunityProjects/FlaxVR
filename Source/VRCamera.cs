using System;
using System.Collections.Generic;
using System.ComponentModel;
using FlaxEngine;
using FlaxVR.OpenVR;

namespace FlaxVR
{
    public class VRCamera : Script
    {

        private static readonly string trackingSpaceName = "TrackingSpace";
        private static readonly string trackerAnchorName = "TrackerAnchor";
        private static readonly string leftControllerAnchorName = "LeftControllerAnchor";
        private static readonly string rightControllerAnchorName = "RightControllerAnchor";

        VRContext _context;

        CustomRenderTask renderTask;

        RenderBuffers lRenderBuffers;
        RenderBuffers rRenderBuffers;

        RenderView lRenderView;
        RenderView rRenderView;
        /*BoundingFrustum lBoundingFrustum;
        BoundingFrustum rBoundingFrustum;*/

        [Serialize]
        private float _zNear = 8f;
        [Serialize]
        private float _zFar = 20000f;

        public bool MirrorEnabled = true;
        [VisibleIf("MirrorEnabled")]
        public MirrorTextureEyeSource MirrorSource = MirrorTextureEyeSource.BothEyes;

        private UICanvas canvas;
        private VRMirrorBlitter blitter;
        private bool _hasNewPoses = false;

        private List<VRController> vrControllers = new List<VRController>();

        [HideInEditor]
        public VRController LeftController;

        [HideInEditor]
        public VRController RightController;

        // Anchors
        [HideInEditor]
        public EmptyActor TrackingSpace { get; private set; }

        [HideInEditor]
        public EmptyActor TrackerAnchor { get; private set; }

        [HideInEditor]
        public EmptyActor LeftControllerAnchor { get; private set; }

        [HideInEditor]
        public EmptyActor RightControllerAnchor { get; private set; }

        [Range(0.1f, 30000f)]
        [DefaultValue(8f)]
        [EditorOrder(0)]
        public float ZNear
        {
            get => _zNear;
            set
            {
                if (_zNear != value)
                {
                    _zNear = value;
                    _context?.UpdateProjectionMatrices(_zNear, _zFar);
                }
            }
        }

        [Range(0.1f, 30000f)]
        [DefaultValue(20000f)]
        [EditorOrder(1)]
        public float ZFar
        {
            get => _zFar;
            set
            {
                if (_zFar != value)
                {
                    _zFar = value;
                    _context?.UpdateProjectionMatrices(_zNear, _zFar);
                }
            }
        }

        public override void OnUpdate()
        {
            if (_context != null)
            {
                // Clone settings from MainRenderTask (LOD, Quality etc..)
                var mrtView = MainRenderTask.Instance.View;
                lRenderView = rRenderView = mrtView;

                // Get new poses
                var poses = _context.WaitForPoses();

                // TODO: Move actor itself here and update controllers

                // Update left eye
                var leftView = poses.CreateView(VREye.Left, Actor.Position, Vector3.UnitZ, Vector3.UnitY, out Vector3 lEyePos, out Vector3 lDirection);
                var leftProjection = poses.LeftEyeProjection;
                //lBoundingFrustum = new BoundingFrustum(leftView * leftProjection);
                lRenderView.SetUp(ref leftView, ref leftProjection);
                // And set the position and direction
                lRenderView.Position = lEyePos;
                lRenderView.Direction = lDirection;
                lRenderView.Flags = lRenderView.Flags & ~ViewFlags.MotionBlur;

                // Update right eye
                var rightView = poses.CreateView(VREye.Right, Actor.Position, Vector3.UnitZ, Vector3.UnitY, out Vector3 rEyePos, out Vector3 rDirection);
                var rightProjection = poses.RightEyeProjection;
                //rBoundingFrustum = new BoundingFrustum(rightView * rightProjection);
                rRenderView.SetUp(ref rightView, ref rightProjection);
                // And set the position and direction
                rRenderView.Position = rEyePos;
                rRenderView.Direction = rDirection;
                rRenderView.Flags = rRenderView.Flags & ~ViewFlags.MotionBlur;

                canvas.IsActive = MirrorEnabled;

                if (blitter != null)
                    blitter.MirrorMode = MirrorSource;

                _hasNewPoses = true;

                _context.UpdateDevices();
            }
        }


        private EmptyActor ConfigureRootAnchor(string name)
        {
            EmptyActor anchor = Actor.GetChild(name) as EmptyActor;
            if (anchor == null)
            {
                anchor = EmptyActor.New();
                Actor.AddChild(anchor);
                anchor.Name = name;
            }
            return anchor;
        }

        private EmptyActor ConfigureTrackerAnchor(EmptyActor root)
        {
            EmptyActor anchor = Actor.GetChild(trackerAnchorName) as EmptyActor;
            if (anchor == null)
            {
                anchor = EmptyActor.New();
                root.AddChild(anchor);
                anchor.Name = trackerAnchorName;
            }
            return anchor;
        }

        private EmptyActor ConfigureControllerAnchor(EmptyActor root, VRControllerRole role)
        {
            string name = role == VRControllerRole.LeftHand ? leftControllerAnchorName : role == VRControllerRole.RightHand ? rightControllerAnchorName : string.Empty;

            EmptyActor anchor = Actor.GetChild(name) as EmptyActor;

            if (anchor == null)
            {
                anchor = EmptyActor.New();
                var script = New<VRController>();
                script.Role = role;
                anchor.AddScript(script);
                root.AddChild(anchor);
                anchor.Name = name;
            }
            return anchor;
        }

        private void SetupHierarchy()
        {
            if (TrackingSpace == null)
            {
                TrackingSpace = ConfigureRootAnchor(trackingSpaceName);
            }

            if (TrackerAnchor == null)
            {
                TrackerAnchor = ConfigureTrackerAnchor(TrackingSpace);
            }

            if (LeftControllerAnchor == null)
            {
                LeftControllerAnchor = ConfigureControllerAnchor(TrackingSpace, VRControllerRole.LeftHand);
                LeftController = LeftControllerAnchor.GetScript<VRController>();
            }

            if (RightControllerAnchor == null)
            {
                RightControllerAnchor = ConfigureControllerAnchor(TrackingSpace, VRControllerRole.RightHand);
                RightController = RightControllerAnchor.GetScript<VRController>();
            }


            vrControllers = new List<VRController>(Actor.GetScriptsInChildren<VRController>());

        }


        public override void OnLateUpdate()
        {

            if (_context != null)
            {

                foreach (VRController controller in vrControllers)
                {
                    VRControllerRole role = controller.Role;
                    int cIndex;
                    if (role == VRControllerRole.LeftHand)
                    {
                        cIndex = _context.LeftControllerIndex;
                    }
                    else if (role == VRControllerRole.RightHand)
                    {
                        cIndex = _context.RightControllerIndex;
                    }
                    else
                    {
                        cIndex = controller.ControllerIndex;
                    }

                    VRControllerState[] states = _context.Controllers.ToArray();
                    VRControllerState newState;

                    if (cIndex >= 0 && states != null && cIndex < states.Length)
                    {
                        newState = states[cIndex];
                    }
                    else
                    {
                        newState = new VRControllerState();
                    }

                    controller.UpdateState(newState);
                }
            }
        }

        /*private void OnDebugDraw()
        {
            DebugDraw.DrawWireFrustum(lBoundingFrustum, Color.Red);
            DebugDraw.DrawWireFrustum(rBoundingFrustum, Color.Blue);
        }*/

        public override void OnStart()
        {
            if (VRContext.IsOpenVRSupported())
            {
                var options = new VRContextOptions
                {
                    EyeRenderTargetSampleCount = MSAALevel.X4
                };
                _context = VRContext.CreateOpenVR(options);
            }
            else
                throw new Exception("OpenVR is not supported");

            _context.Initialize();

            MainRenderTask.Instance.Enabled = false;


            renderTask = FlaxEngine.Object.New<CustomRenderTask>();
            renderTask.Render += (ctx) =>
            {

                // Only draw after WaitForPoses otherwise we get "frame already submited" exceptions 
                // TODO: Run pose update seaparate from standard update loop (probably in render task or sth)
                // So we update poses as often as possible
                if (!_hasNewPoses)
                    return;

                _hasNewPoses = false;


                if (lRenderBuffers == null)
                    lRenderBuffers = RenderBuffers.New();

                if (rRenderBuffers == null)
                    rRenderBuffers = RenderBuffers.New();

                if (_context.LeftEyeGPUTexture == null || _context.RightEyeGPUTexture == null)
                    return;

                lRenderBuffers.Size = _context.LeftEyeGPUTexture.Size;
                rRenderBuffers.Size = _context.RightEyeGPUTexture.Size;

                lRenderView.Near = ZNear;
                lRenderView.Far = ZFar;
                rRenderView.Near = ZNear;
                rRenderView.Far = ZFar;
                ctx.DrawScene(renderTask, _context.LeftEyeGPUTexture, lRenderBuffers, ref lRenderView, Utils.GetEmptyArray<Actor>(), ActorsSources.Scenes, null);
                ctx.DrawScene(renderTask, _context.RightEyeGPUTexture, rRenderBuffers, ref rRenderView, Utils.GetEmptyArray<Actor>(), ActorsSources.Scenes, null);

                _context?.SubmitFrame();
            };

            canvas = Actor.GetOrAddChild<UICanvas>();
            canvas.RenderMode = CanvasRenderMode.ScreenSpace;
            canvas.HideFlags = HideFlags.FullyHidden;

            blitter = canvas.GUI.GetChild<VRMirrorBlitter>() ?? canvas.GUI.AddChild(new VRMirrorBlitter(_context));

            MainRenderTask.Instance.Begin += UpdateMRT;

            SetupHierarchy();
        }

        private void UpdateMRT(SceneRenderTask task, GPUContext context)
        {
            task.ActorsSource = MirrorEnabled ? ActorsSources.CustomActors : ActorsSources.Scenes;
            if (MirrorEnabled && !task.CustomActors.Contains(canvas))
                task.CustomActors.Add(canvas);

            if (!MirrorEnabled)
                task.CustomActors.Clear();
        }

        public override void OnDestroy()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }


            MainRenderTask.Instance.CustomActors.Clear();
            MainRenderTask.Instance.ActorsSource = ActorsSources.Scenes;
            MainRenderTask.Instance.Begin -= UpdateMRT;

            if (renderTask != null)
            {
                renderTask.Dispose();
                renderTask = null;
            }

            if (lRenderBuffers != null)
            {
                lRenderBuffers.Dispose();
                lRenderBuffers = null;
            }

            if (rRenderBuffers != null)
            {
                rRenderBuffers.Dispose();
                rRenderBuffers = null;
            }

            if (blitter != null)
            {
                blitter = null;
            }

            if (canvas != null)
            {
                Destroy(canvas);
                canvas = null;
            }
        }
    }
}
