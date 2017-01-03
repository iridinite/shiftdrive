﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an animated sprite.
    /// </summary>
    internal sealed class SpriteSheet {

        /// <summary>
        /// Specifies a style with which to render a sprite.
        /// </summary>
        internal enum SpriteBlend {
            AlphaBlend,
            Additive,
            HalfBlend
        }

        /// <summary>
        /// Represents a single frame in the animation.
        /// </summary>
        internal class SpriteFrame {
            public Texture2D texture;
            public float hold;
        }

        /// <summary>
        /// Represents a texture layer in a layered sprite.
        /// </summary>
        internal class SpriteLayer {
            public string tag;

            public List<SpriteFrame> frames = new List<SpriteFrame>();
            public SpriteBlend blend;
            public float rotate;
            public float rotateSpeed;
            public float scale;

            public int frameNo;
            public float frameTime;
        }

        public List<SpriteLayer> layers = new List<SpriteLayer>();
        public bool isPrototype;
        private bool offsetRandom;

        /// <summary>
        /// Reads a sprite sheet prototype from a file.
        /// </summary>
        /// <param name="filename">The path to the file.</param>
        public static SpriteSheet FromFile(string filename) {
            SpriteSheet ret = new SpriteSheet();
            ret.isPrototype = true;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            // TODO: maybe do these integrity checks with an actual XML schema document?
            XmlNode xmlRoot = xmlDoc.DocumentElement;
            if (xmlRoot == null || !String.Equals(xmlRoot.Name, "sprite", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidDataException($"In sprite sheet '{filename}': document must have exactly one root node named 'sprite'");

            if (!xmlRoot.HasChildNodes)
                throw new InvalidDataException($"In sprite sheet '{filename}': sprite must have at least one layer");

            // helper for getting attribute and dealing with defaults / invalid data
            Func<XmlNode, string, float, float> getFloatAttr = (node, attrname, defval) => {
                float outval;
                XmlAttribute attr = node.Attributes?[attrname];
                if (attr == null) return defval;
                if (float.TryParse(attr.Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture.NumberFormat, out outval))
                    return outval;
                throw new InvalidDataException($"In sprite sheet '{filename}': in node '{node.Name}': attribute '{attrname}' has invalid value '{attr.Value}'");
            };

            // walk through all layer definitions
            XmlNode xmlLayer = xmlRoot.FirstChild;
            while (xmlLayer != null) {
                // parse basic layer settings
                SpriteLayer layer = new SpriteLayer();
                layer.tag = xmlLayer.Attributes?["tag"]?.Value;
                layer.rotateSpeed = getFloatAttr(xmlLayer, "rotate", 0f);
                layer.scale = getFloatAttr(xmlLayer, "scale", 1f);

                // parse blend mode string
                string blendmode = xmlLayer.Attributes?["blend"]?.Value ?? "alpha";
                if (blendmode.Equals("alpha", StringComparison.InvariantCultureIgnoreCase))
                    layer.blend = SpriteBlend.AlphaBlend;
                else if (blendmode.Equals("additive", StringComparison.InvariantCultureIgnoreCase))
                    layer.blend = SpriteBlend.Additive;
                else if (blendmode.Equals("half", StringComparison.InvariantCultureIgnoreCase))
                    layer.blend = SpriteBlend.HalfBlend;
                else 
                    throw new InvalidDataException($"In sprite sheet '{filename}': in layer '{layer.tag ?? "unnamed"}': invalid blend mode '{blendmode}'");

                if (!xmlLayer.HasChildNodes)
                    throw new InvalidDataException($"In sprite sheet '{filename}': in layer '{layer.tag ?? "unnamed"}': must have at least one frame");

                // parse the collection of frames
                XmlNode xmlFrame = xmlLayer.FirstChild;
                while (xmlFrame != null) {
                    if (xmlFrame.Attributes?["texture"] == null)
                        throw new InvalidDataException($"In sprite sheet '{filename}': in layer '{layer.tag ?? "unnamed"}': frame must have texture attribute");

                    SpriteFrame frame = new SpriteFrame();
                    frame.texture = Assets.GetTexture(xmlFrame.Attributes["texture"].Value);
                    frame.hold = getFloatAttr(xmlFrame, "hold", 1f);
                    layer.frames.Add(frame);
                    xmlFrame = xmlFrame.NextSibling;
                }

                // save this layer and continue to the next
                ret.layers.Add(layer);
                xmlLayer = xmlLayer.NextSibling;
            }

            return ret;
        }

        /// <summary>
        /// Instantiates a copy of this <see cref="SpriteSheet"/> that can be tied to a specific <seealso cref="GameObject"/>.
        /// </summary>
        /// <returns></returns>
        public SpriteSheet Clone() {
            if (!isPrototype) throw new InvalidOperationException("Cannot clone instantiated sprite sheet. Clone a prototype instead.");

            SpriteSheet ret = new SpriteSheet();
            ret.isPrototype = false;
            ret.layers = new List<SpriteLayer>();
            foreach (SpriteLayer protlayer in layers) {
                SpriteLayer clonelayer = new SpriteLayer();
                clonelayer.tag = protlayer.tag;
                clonelayer.blend = protlayer.blend;
                clonelayer.frames = protlayer.frames;
                clonelayer.frameTime = protlayer.frameTime;
                clonelayer.frameNo = 0;
                clonelayer.rotate = 0f;
                clonelayer.rotateSpeed = protlayer.rotateSpeed;
                clonelayer.scale = protlayer.scale;
                ret.layers.Add(clonelayer);
            }
            if (offsetRandom) // randomize offsets
                foreach (SpriteLayer layer in ret.layers)
                    layer.frameTime = (float)Utils.RNG.NextDouble() * layer.frames[0].hold;
            return ret;
        }

        /// <summary>
        /// Returns a specified <see cref="SpriteLayer"/> from this sheet.
        /// </summary>
        /// <param name="index">The layer index to look up.</param>
        public SpriteLayer GetLayer(int index) {
            return layers[index];
        }

        /// <summary>
        /// Updates this sprite sheet, advancing the animation as necessary.
        /// </summary>
        /// <param name="deltaTime">The time that passed since the last call to Update.</param>
        public void Update(float deltaTime) {
            if (isPrototype) return;

            foreach (SpriteLayer layer in layers) {
                // advance rotation speed
                layer.rotate += layer.rotateSpeed * deltaTime;

                // add delta time to this frame's lifetime
                layer.frameTime += deltaTime;
                if (!(layer.frameTime > layer.frames[layer.frameNo].hold)) continue;

                // if the wait time is over, advance the frame
                layer.frameNo++;
                layer.frameTime = 0f;
                if (layer.frameNo >= layer.frames.Count) layer.frameNo = 0;
            }
        }

    }

}
