using UnityEngine;

namespace CustomIonCubes
{
    /// <summary>
    /// Contains all the data necessary for an ion cube with a custom colour.
    /// </summary>
    /// <seealso cref="CustomCubeHandler.RegisterCube(CubeColor)"/>
    public class CubeColor
    {
        /// <summary>
        /// The internal id used for registering.
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Determines the main color of the texture. Changing this value will have the largest impact on the overall
        /// look.
        /// </summary>
        public Color MainColor;
        
        /// <summary>
        /// This color mostly impacts the glowy highlights like the "energy lines" running through the cube.
        /// </summary>
        public Color Details;
        
        /// <summary>
        /// Determines the look of the many animated squares that flash over the surface of the cube. Note
        /// that the game takes this color as a baseline for the random colors it *actually* displays.
        /// </summary>
        public Color AnimatedSquares;
        
        /// <summary>
        /// Determines how much the cube glows, i.e. how visible it is at night. Different colors have little impact
        /// here, the alpha channel makes all the difference.
        /// </summary>
        public Color Glow;
        
        /// <summary>
        /// Determines the color of the light used to illuminate the surroundings of the cube. Rarely visible unless
        /// the cube is resting on a surface.
        /// </summary>
        public Color Illumination;
    }
}