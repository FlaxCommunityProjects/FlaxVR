using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxVR
{
    internal class VRMirrorBlitter : Control
    {
        public MirrorTextureEyeSource MirrorMode;
        private readonly VRContext _context;

        public VRMirrorBlitter(VRContext ctx)
        {
            _context = ctx;
        }

        public override void Draw()
        {
            var size = this.Parent.Size;

            Render2D.FillRectangle(new Rectangle(0, 0, size.X, size.Y), Color.Black);

            switch (MirrorMode)
            {
                case MirrorTextureEyeSource.BothEyes:
                    float w = size.X * 0.5f;
                    BlitEye(_context.LeftEyeGPUTexture, 0, 0, w, size.Y, true, false);
                    BlitEye(_context.RightEyeGPUTexture, w, 0, w, size.Y, false, true);
                    break;
                case MirrorTextureEyeSource.LeftEye:
                    BlitEye(_context.LeftEyeGPUTexture, 0, 0, size.X, size.Y);
                    break;
                case MirrorTextureEyeSource.RightEye:
                    BlitEye(_context.RightEyeGPUTexture, 0, 0, size.X, size.Y);
                    break;
            }
        }

        private void BlitEye(GPUTexture gt, float x, float y, float w, float h, bool horizontalRight = false, bool horizontalLeft = false)
        {
            float rtAspectRatio = gt.Width / (float)gt.Height;
            float aspectRatio = w / h;

            float sampleWidth;
            float sampleHeight;

            if (aspectRatio > 1)
            {
                sampleHeight = h;
                sampleWidth = h * rtAspectRatio;
            }
            else
            {
                sampleWidth = w;
                sampleHeight = w / rtAspectRatio;
            }

            float posX = x + w * 0.5f - sampleWidth * 0.5f;
            float posY = y + h * 0.5f - sampleHeight * 0.5f;

            if (horizontalLeft)
                posX = x;

            if (horizontalRight)
                posX = x + w - sampleWidth;

            Render2D.DrawTexture(gt, new Rectangle(posX, posY, sampleWidth, sampleHeight), Color.White);
        }
    }
}
