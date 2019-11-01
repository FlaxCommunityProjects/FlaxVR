using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVR
{
    /// <summary>
    /// OpenVR Tracking reference
    /// </summary>
    public class VRTrackingReference
    {
        /// <summary>
        /// The position of the pose
        /// </summary>
        public Vector3 Postion { get; set; }

        /// <summary>
        /// The orientation of the pose
        /// </summary>
        public Quaternion Orientation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this pose is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this pose is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Updates the specified pose.
        /// </summary>
        internal void Update(Vector3 position, Quaternion orientation)
        {
            IsConnected = true;
            Postion = position;
            Orientation = orientation;
        }
    }
}
