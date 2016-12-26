/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a fire-and-forget particle effect.
    /// </summary>
    internal sealed class Particle {
        
        public Vector2 position;
        public Vector2 velocity;
        public float facing;
        
        public SpriteSheet sprite;
        public byte zorder;

        public float life;
        public float lifemax;

        public float scalestart;
        public float scaleend;
        public float rotatespeed;
        public float rotateoffset;

        public Color colorstart;
        public Color colorend;

        public Particle() {
            life = 0f;
            lifemax = 0f;
            scalestart = 1f;
            scaleend = 1f;
            rotatespeed = 0f;
            rotateoffset = 0f;
            colorstart = Color.White;
            colorend = Color.White;
            zorder = 16;
        }
        
    }

}
