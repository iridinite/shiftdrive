/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a shielded spaceship.
    /// </summary>
    internal abstract class Ship : NamedObject {

        public const int WEAPON_ARRAY_SIZE = 8;

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Health)]
        public float Hull { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.HealthMax)]
        public float HullMax { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Health)]
        public float Shield { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.HealthMax)]
        public float ShieldMax { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Health)]
        public bool ShieldActive { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.MoveStats)]
        public float TopSpeed { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.MoveStats)]
        public float TurnRate { get; protected set; }

        [ScriptableProperty(ScriptAccess.Read)]
        public byte WeaponsCount { get; private set; }

        [ScriptableProperty(ScriptAccess.Write, ObjectProperty.Mounts)]
        public WeaponMount[] Mounts {
            get { return mounts; }
            set {
                Debug.Assert(value != null && value.Length == WEAPON_ARRAY_SIZE);
                mounts = value;
                WeaponsCount = (byte)mounts.Count(p => p != null);
            }
        }

        [ScriptableProperty(ScriptAccess.Write, ObjectProperty.Weapons)]
        public Weapon[] Weapons { get; protected set; }

        [ScriptableProperty(ScriptAccess.Write, ObjectProperty.Flares)]
        public Vector2[] Trails { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Throttle)]
        public float Throttle { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Steering)]
        public float Steering { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Faction)]
        public byte Faction { get; protected set; }

        private WeaponMount[] mounts;
        private float flaretime;
        private float shieldRegenPause;
        private Particle shieldBubble;

        protected Ship(GameState world) : base(world) {
            Hull = 100f;
            HullMax = 100f;
            Shield = 100f;
            ShieldMax = 100f;
            ShieldActive = false;
            Damping = 0.75f;
            Mounts = new WeaponMount[WEAPON_ARRAY_SIZE];
            Weapons = new Weapon[WEAPON_ARRAY_SIZE];
            flaretime = 0f;
            shieldRegenPause = 0f;
            ZOrder = 128;
            Layer = CollisionLayer.Ship;
            LayerMask = CollisionLayer.Ship | CollisionLayer.Asteroid | CollisionLayer.Default;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            Velocity = Vector2.Zero;

            // apply throttle velocity based on the ship's facing
            Vector2 movementByEngine = new Vector2(
                    (float)Math.Cos(MathHelper.ToRadians(Facing - 90f)),
                    (float)Math.Sin(MathHelper.ToRadians(Facing - 90f)))
                * Throttle * TopSpeed;
            Movement += movementByEngine;
            Position += movementByEngine * deltaTime;
            //position.X = MathHelper.Clamp(position.X, 0f, 1000f);
            //position.Y = MathHelper.Clamp(position.Y, 0f, 1000f);
            // apply maneuver: find whether turning left or right is fastest
            float deltaFacing = MathHelper.Clamp(Utils.Repeat((Steering - Facing) + 180, 0f, 360f) - 180f, -1f, 1f);
            Facing = Utils.Repeat(Facing + deltaFacing * TurnRate * deltaTime, 0f, 360f);

            // retransmit modified properties
            if (Throttle > 0f)
                Changed |= ObjectProperty.Position;
            if (Math.Abs(deltaFacing) > 0.001f)
                Changed |= ObjectProperty.Facing;

            // shield regeneration
            if (Shield < ShieldMax) {
                if (shieldRegenPause > 0f)
                    shieldRegenPause -= deltaTime;
                else {
                    Shield = Math.Min(Shield + deltaTime / 2f, ShieldMax);
                    Changed |= ObjectProperty.Health;
                }
            }

            // update weapon charge / ammo states
            for (int i = 0; i < WeaponsCount; i++) {
                Weapon wep = Weapons[i];
                if (wep == null) continue;

                // sanity check, active weapons must have a mount
                Debug.Assert(Mounts[i] != null);
                // charge and fire the weapon
                wep.Update(deltaTime, this, Mounts[i]);
            }

            // update mount point position
            for (int i = 0; i < WeaponsCount; i++) {
                if (Mounts[i] == null) continue;
                Mounts[i].Position = Utils.CalculateRotatedOffset(Mounts[i].Offset, Facing);
            }

            // engine flares
            if (!(Throttle > 0f) || World.IsServer) return;
            if (flaretime > 0f) {
                // space out evenly
                flaretime -= deltaTime;
                return;
            }
            flaretime = 0.01f;
            // create particles for engine exhaust
            foreach (Vector2 flarepos in Trails) {
                Particle flare = new Particle();
                flare.lifemax = 3f;
                flare.sprite = Assets.GetSprite("map/engineflare").Clone();
                flare.scalestart = 1.1f;
                flare.scaleend = 0.9f;
                flare.colorstart = Color.White * (Throttle * 0.6f + 0.15f);
                flare.colorend = Color.Transparent;
                flare.facing = Facing;
                flare.zorder = 160;
                flare.position = Position + Utils.CalculateRotatedOffset(flarepos, Facing);
                ParticleManager.Register(flare);
            }
        }

        public override void TakeDamage(float damage, bool sound = false) {
            // need to resend hull and shields
            Changed |= ObjectProperty.Health;

            // delay shield recharge
            shieldRegenPause = 10f;

            // apply damage to shields first, if possible
            if (ShieldActive && Shield > 0f) {
                // sound effect on server
                if (World.IsServer && sound)
                    Assets.GetSound("DamageShield").Play3D(World.GetPlayerShip().Position, Position);
                // apply damage
                ShowShieldBubble();
                Shield = MathHelper.Clamp(Shield - damage, 0f, ShieldMax);
                return;
            }
            // otherwise, apply damage to hull
            if (World.IsServer && sound)
                Assets.GetSound("DamageHull").Play3D(World.GetPlayerShip().Position, Position);
            Hull = MathHelper.Clamp(Hull - damage, 0f, HullMax);
            // zero hull = ship destruction
            if (Hull <= 0f && World.IsServer) Destroy();
        }

        public override void Destroy() {
            if (!IsDestroyScheduled()) {
                Assets.GetSound("ExplosionMedium").Play3D(World.GetPlayerShip().Position, Position);
                NetServer.PublishParticleEffect(ParticleEffect.Explosion, Position);
            }
            base.Destroy();
        }

        public override bool IsTargetable() {
            return true;
        }

        public bool IsAlly(Ship other) {
            return Faction == other.Faction;
        }

        public bool IsNeutral() {
            return Faction == 0;
        }

        public Color GetFactionColor(Ship observer) {
            if (IsNeutral()) return Color.CornflowerBlue;
            return IsAlly(observer) ? Color.LightGreen : Color.Red;
        }

        private void ShowShieldBubble() {
            if (World.IsServer) return;

            // create a bubble effect if none exists or the old one expired
            if (shieldBubble == null || shieldBubble.life >= shieldBubble.lifemax)
                shieldBubble = ParticleManager.CreateShieldBubble(this);

            // reset the life of the bubble to make it appear at max brightness again
            shieldBubble.life = 0.0f;
        }

        public virtual GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject target = null;
            float closest = float.MaxValue;

            // find closest object. station has 360 weapon so don't care about weapon arcs
            foreach (GameObject gobj in World.Objects.Values) {
                // make sure we can actually shoot this thing
                if (!GetCanTarget(gobj, weapon.Range, mount.Bearing + this.Facing, mount.Arc))
                    continue;

                // keep closest object
                float dist = Vector2.DistanceSquared(gobj.Position, this.Position);
                if (dist > closest) continue;

                closest = dist;
                target = gobj;
            }

            return target;
        }

        protected bool GetCanTarget(GameObject target, float range, float bearing, float arc) {
            // must be targetable at all
            if (!target.IsTargetable())
                return false;

            // if ship, cannot fire on friendlies
            if (target.IsShip()) {
                Ship ship = target as Ship;
                Debug.Assert(ship != null);
                if (ship.IsAlly(this))
                    return false;
            }

            // cannot exceed weapon range
            if (Vector2.DistanceSquared(target.Position, this.Position) > range * range)
                return false;

            // cannot fall outside weapon arc
            float targetAngle = Utils.CalculateBearing(this.Position, target.Position);
            float arcfrom = Utils.Repeat(bearing - arc, 0f, 360f);
            float arcto = Utils.Repeat(bearing + arc, 0f, 360f);

            while (arcto < arcfrom) arcto += 360f;
            while (targetAngle < arcfrom) targetAngle += 360f;

            return targetAngle >= arcfrom && targetAngle <= arcto;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // resolve collision
            base.OnCollision(other, normal, penetration);

            // find the highest velocity involved in the collision
            float highestVelocity = Throttle * TopSpeed * Math.Abs(penetration);
            // if colliding with another ship, factor in that ship's speed
            Ship otherShip = other as Ship;
            if (otherShip != null)
                highestVelocity = Math.Max(highestVelocity,
                    otherShip.Throttle * otherShip.TopSpeed * Math.Abs(penetration));

            // cap damage and apply
            float damage = Math.Min(highestVelocity * SDGame.Inst.GetDeltaTime() * 2, 0.25f);
            TakeDamage(damage);

            // TODO: collision sounds

            if (other.Type == ObjectType.Asteroid) {
                // reduce pushback
                Velocity *= 0.5f;
            }
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            // hull and shield status
            if (Changed.HasFlag(ObjectProperty.Health)) {
                outstream.Write(Hull);
                outstream.Write(Shield);
                outstream.Write(ShieldActive);
            }

            if (Changed.HasFlag(ObjectProperty.HealthMax)) {
                outstream.Write(HullMax);
                outstream.Write(ShieldMax);
            }

            // movement
            if (Changed.HasFlag(ObjectProperty.Throttle))
                outstream.Write(Throttle);
            if (Changed.HasFlag(ObjectProperty.Steering))
                outstream.Write(Steering);
            if (Changed.HasFlag(ObjectProperty.MoveStats)) {
                outstream.Write(TopSpeed);
                outstream.Write(TurnRate);
            }

            // mounts and weapons data
            if (Changed.HasFlag(ObjectProperty.Mounts)) {
                outstream.Write(WeaponsCount);
                for (int i = 0; i < WeaponsCount; i++)
                    Mounts[i].Serialize(outstream);
            }
            if (Changed.HasFlag(ObjectProperty.Weapons)) {
                outstream.Write(WeaponsCount);
                for (int i = 0; i < WeaponsCount; i++)
                    Weapons[i].Serialize(outstream);
            }

            // engine flare positions
            if (Changed.HasFlag(ObjectProperty.Flares)) {
                if (Trails != null) {
                    outstream.Write((byte)Trails.Length);
                    for (int i = 0; i < Trails.Length; i++) {
                        outstream.Write(Trails[i].X);
                        outstream.Write(Trails[i].Y);
                    }
                } else {
                    outstream.Write((byte)0);
                }
            }

            // combat faction
            if (Changed.HasFlag(ObjectProperty.Faction))
                outstream.Write(Faction);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.Health)) {
                Hull = instream.ReadSingle();
                Shield = instream.ReadSingle();
                ShieldActive = instream.ReadBoolean();
            }

            if (recvChanged.HasFlag(ObjectProperty.HealthMax)) {
                HullMax = instream.ReadSingle();
                ShieldMax = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Throttle))
                Throttle = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Steering))
                Steering = instream.ReadSingle();

            if (recvChanged.HasFlag(ObjectProperty.MoveStats)) {
                TopSpeed = instream.ReadSingle();
                TurnRate = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Mounts)) {
                byte mountsNum = instream.ReadByte();
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (i >= mountsNum)
                        Mounts[i] = null;
                    else
                        Mounts[i] = WeaponMount.FromStream(instream);
                }
            }

            if (recvChanged.HasFlag(ObjectProperty.Weapons)) {
                WeaponsCount = instream.ReadByte();
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (i >= WeaponsCount)
                        Weapons[i] = null;
                    else
                        Weapons[i] = Weapon.FromStream(instream);
                }
            }

            if (recvChanged.HasFlag(ObjectProperty.Flares)) {
                int flaresCount = instream.ReadByte();
                Trails = new Vector2[flaresCount];
                for (int i = 0; i < flaresCount; i++)
                    Trails[i] = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            }

            if (recvChanged.HasFlag(ObjectProperty.Faction))
                Faction = instream.ReadByte();
        }

    }

}
