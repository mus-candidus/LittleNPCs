using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Locations;


namespace LittleNPCs.Framework {
    public class LittleNPC : NPC {
        private IMonitor monitor_;

        public long IdOfParent { get; private set; }

        public int ChildIndex { get; private set; }

        public LittleNPC(IMonitor monitor, long idOfParent, int childIndex, AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDir, string name, Dictionary<int, int[]> schedule, Texture2D portrait, bool eventActor)
        : base(sprite, position, defaultMap, facingDir, name, schedule, portrait, eventActor, null) {
            monitor_ = monitor;
            IdOfParent = idOfParent;
            ChildIndex = childIndex;
        }
    }
}
