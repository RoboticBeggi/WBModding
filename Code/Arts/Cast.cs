using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// One use of a technique, and the choreography toolkit its script directs it with. Everything runs on
    /// effect time, so scripts freeze during hit-stop and slow down in cutscenes.
    ///
    /// Conventions for scripts:
    /// - Art sets are named without the "arts/" prefix: F("rock_pillar", …). Old shared shapes use "fx:" (F("fx:slash", …)).
    /// - Most art sets carry their own colours; pass tint: true for the white ones (impact, shock, breath,
    ///   claw, wingbeat, roar) or any time you want the style colour.
    /// - Sounds are named without a folder: Snd("stone_rise").
    /// - Scale effects with L(i, ii, iii) so Level II and III look and hit visibly bigger.
    /// </summary>
    public class Cast
    {
        public Actor A;
        public Actor T;
        public Technique Tech;
        public MoveSet Style;

        /// <summary>Mastery level, 1–3.</summary>
        public int Lv = 1;

        /// <summary>Damage multiplier from Resonance, mastery and team-ups (scripts multiply their own power by it).</summary>
        public float Pow = 1f;

        public string Element;

        /// <summary>The style colour, and its white-hot Level III form.</summary>
        public Color Col, Hot;

        public bool God;

        private Vector3 _last_tp;

        public Cast(Actor pCaster, Actor pTarget, Technique pTech, MoveSet pStyle, int pLevel, string pElement)
        {
            A = pCaster;
            T = pTarget;
            Tech = pTech;
            Style = pStyle;
            Lv = Mathf.Clamp(pLevel, 1, 3);
            Element = pElement;
            Col = Elements.Color(pElement);
            Hot = Lv >= 3 ? Color.Lerp(Col, Color.white, 0.5f) : Col;
            _last_tp = pTarget != null ? (Vector3)pTarget.current_position : (Vector3)pCaster.current_position;
        }

        // ---------------------------------------------------------------- state

        public bool Ok => CombatCore.Alive(A);
        public bool TOk => CombatCore.Alive(T);

        public Vector3 P => A != null ? (Vector3)A.current_position : _last_tp;

        /// <summary>The target's position, or where it was last seen.</summary>
        public Vector3 TP
        {
            get
            {
                if (TOk) _last_tp = T.current_position;
                return _last_tp;
            }
        }

        public float Dist => Vector2.Distance(P, TP);

        /// <summary>Unit direction from caster to target (or the way the caster faces).</summary>
        public Vector3 Dir
        {
            get
            {
                Vector3 d = TP - P;
                if (d.sqrMagnitude < 0.0001f) return A != null && A.flip ? Vector3.left : Vector3.right;
                return d.normalized;
            }
        }

        /// <summary>Angle in degrees of <see cref="Dir"/>.</summary>
        public float Ang => Mathf.Atan2(Dir.y, Dir.x) * Mathf.Rad2Deg;

        public static float AngleOf(Vector3 pDir) => Mathf.Atan2(pDir.y, pDir.x) * Mathf.Rad2Deg;

        public static Vector3 Rot(Vector3 pDir, float pDegrees) => Quaternion.Euler(0f, 0f, pDegrees) * pDir;

        /// <summary>Picks a value by level.</summary>
        public float L(float i, float ii, float iii) => Lv >= 3 ? iii : Lv == 2 ? ii : i;

        public int Li(int i, int ii, int iii) => Lv >= 3 ? iii : Lv == 2 ? ii : i;

        /// <summary>Tinted colour for this level (white-hot at III).</summary>
        public Color C => Hot;

        public Color Fade(float pAlpha) => new Color(Hot.r, Hot.g, Hot.b, pAlpha);

        public Vector3 Front(float pDistance) => P + Dir * pDistance;

        /// <summary>A point on the far side of a unit, seen from the caster.</summary>
        public Vector3 Behind(Actor pVictim, float pDistance = 0.9f)
        {
            Vector3 v = pVictim != null ? (Vector3)pVictim.current_position : TP;
            Vector3 d = v - P;
            if (d.sqrMagnitude < 0.0001f) d = Vector3.right;
            return Clamp(v + d.normalized * pDistance);
        }

        public static Vector3 Clamp(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, 1f, MapBox.width - 2f), Mathf.Clamp(p.y, 1f, MapBox.height - 2f), 0f);

        public static Vector3 Rand(float pRadius) => (Vector3)(Random.insideUnitCircle * pRadius);

        public IEnumerator W(float pSeconds) => CombatCore.Wait(pSeconds);

        // ---------------------------------------------------------------- effects

        private static string SetName(string pSet) => pSet.StartsWith("fx:") ? pSet.Substring(3) : "arts/" + pSet;

        /// <summary>Plays an art set. Size is in tiles; life ≤ 0 plays it once.</summary>
        public Fx.Instance F(string pSet, Vector3 pPos, float pSize, float pRot = 0f, float pLife = 0f,
            bool pLoop = false, float pFps = 16f, bool tint = false, bool back = false)
        {
            if (!Director.OnScreen(pPos)) return null;
            return Fx.Play(SetName(pSet), pPos, pSize, tint ? Hot : Color.white, pRot, pLife, pLoop, pFps,
                back ? "EffectsBack" : "EffectsTop");
        }

        /// <summary>Plays an art set in a chosen colour.</summary>
        public Fx.Instance Fc(string pSet, Vector3 pPos, float pSize, Color pColor, float pRot = 0f, float pLife = 0f,
            bool pLoop = false, float pFps = 16f, bool back = false)
        {
            if (!Director.OnScreen(pPos)) return null;
            return Fx.Play(SetName(pSet), pPos, pSize, pColor, pRot, pLife, pLoop, pFps,
                back ? "EffectsBack" : "EffectsTop");
        }

        /// <summary>A projectile from one point to another. Returns the effect; wait <paramref name="pTime"/> for it to land.</summary>
        public Fx.Instance Shot(string pSet, Vector3 pFrom, Vector3 pTo, float pSize, float pTime, bool tint = false,
            bool rotate = true, float pFps = 16f)
        {
            Fx.Instance fx = Fx.Play(SetName(pSet), pFrom, pSize, tint ? Hot : Color.white, 0f, pTime + 0.04f, true,
                pFps);
            Fx.MoveTo(fx, pTo, pTime, rotate);
            return fx;
        }

        /// <summary>Stretches an art set between two points (beams, cuts, whips, fissures).</summary>
        public Fx.Instance Beam(string pSet, Vector3 pFrom, Vector3 pTo, float pThickness, float pLife,
            bool tint = false, bool loop = true)
        {
            return Fx.Line(SetName(pSet), pFrom, pTo, pThickness, tint ? Hot : Color.white, pLife, loop);
        }

        /// <summary>Glues an effect to a unit.</summary>
        public static void Stick(Fx.Instance pFx, BaseSimObject pTo) => Fx.Attach(pFx, pTo);

        public static void Grow(Fx.Instance pFx, float pFactor) => Fx.Grow(pFx, pFactor);

        /// <summary>A ring of shock and dust: the basic "something heavy happened here".</summary>
        public void Burst(Vector3 pPos, float pSize)
        {
            Fx.Instance shock = F("shock", pPos, pSize, 0f, 0f, false, 22f, true);
            Fx.Grow(shock, 1.3f);
            F("dust", pPos, pSize * 0.8f, 0f, 0f, false, 16f);
        }

        public void Dust(Vector3 pPos, float pSize) => F("dust", pPos, pSize, 0f, 0f, false, 16f);

        /// <summary>A plain hit spark (tinted white-ish impact star).</summary>
        public void Spark(Vector3 pPos, float pSize = 1.2f)
        {
            if (!Director.OnScreen(pPos)) return;
            Fx.Play("arts/impact", pPos, pSize, Color.Lerp(Hot, Color.white, 0.5f), Random.Range(0f, 360f), 0f,
                false, 26f);
        }

        // ---------------------------------------------------------------- sound and text

        public void Snd(string pName, Vector3? pPos = null) => CombatCore.Sound(pName, pPos ?? P, "gi/sfx/arts/");

        /// <summary>Calls out the technique's name over the caster (with its level from II up).</summary>
        public void Call()
        {
            string level = Lv >= 3 ? " III" : Lv == 2 ? " II" : "";
            FloatingText.Show(Tech.Name + level, P + new Vector3(0f, 1.6f), Hot, Lv >= 3 ? 15 : 12);
        }

        public void Say(string pText, Vector3 pPos, int pSize = 12, Color? pColor = null) =>
            FloatingText.Show(pText, pPos, pColor ?? Hot, pSize);

        // ---------------------------------------------------------------- targets and damage

        public List<Actor> Foes(Vector3 pCenter, float pRadius) => CombatCore.EnemiesNear(A, pCenter, pRadius);

        public List<Actor> Allies(float pRadius) => CombatCore.AlliesNear(A, pRadius);

        public Actor NearestFoe(Vector3 pCenter, float pRadius, Actor pExcept = null)
        {
            Actor best = null;
            float bd = pRadius * pRadius;
            foreach (Actor u in Foes(pCenter, pRadius))
            {
                if (u == pExcept) continue;
                float d = ((Vector3)u.current_position - pCenter).sqrMagnitude;
                if (d <= bd) { bd = d; best = u; }
            }
            return best;
        }

        /// <summary>Damages one unit with power × <see cref="Pow"/>. Returns true if it died.</summary>
        public bool Hit(Actor pVictim, float pPower, string pStatus = null, float pStatusTime = 3f)
        {
            if (!Ok || !CombatCore.Alive(pVictim)) return false;
            CombatCore.Hit(A, pVictim, pPower * Pow, Element, pStatus, pStatusTime);
            return !pVictim.isAlive();
        }

        /// <summary>Damages every enemy in a circle. Returns how many were hit.</summary>
        public int Area(Vector3 pCenter, float pRadius, float pPower, string pStatus = null, float pStatusTime = 3f)
        {
            List<Actor> foes = Foes(pCenter, pRadius);
            foreach (Actor u in foes) Hit(u, pPower, pStatus, pStatusTime);
            return foes.Count;
        }

        /// <summary>Damages every enemy along a segment.</summary>
        public int Lane(Vector3 pFrom, Vector3 pTo, float pThickness, float pPower, string pStatus = null)
        {
            List<Actor> foes = CombatCore.EnemiesAlong(A, pFrom, pTo, pThickness);
            foreach (Actor u in foes) Hit(u, pPower, pStatus);
            return foes.Count;
        }

        public void Heal(Actor pUnit, float pFraction)
        {
            if (!CombatCore.Alive(pUnit)) return;
            pUnit.restoreHealth(Mathf.CeilToInt(pUnit.getMaxHealth() * pFraction));
        }

        // ---------------------------------------------------------------- movement

        /// <summary>
        /// Units drawn through their own avatar (dragons, UFOs) are moved by their own components, so the
        /// choreography leaves them where they are and only plays the effects.
        /// </summary>
        public static bool CanMove(Actor pActor) => CombatCore.Alive(pActor) && pActor.avatar == null;

        /// <summary>Holds a unit in place (no AI, no walking) for a while.</summary>
        public static void Hold(Actor pActor, float pSeconds)
        {
            if (CombatCore.Alive(pActor)) pActor.addStatusEffect(Engine.Held, pSeconds);
        }

        public static void Release(Actor pActor)
        {
            if (CombatCore.Alive(pActor)) pActor.finishStatusEffect(Engine.Held);
        }

        /// <summary>A fast slide with afterimages.</summary>
        public IEnumerator Dash(Vector3 pTo, float pSeconds, bool pGhosts = true) =>
            Move(A, pTo, pSeconds, pGhosts ? Hot : (Color?)null);

        public static IEnumerator Move(Actor pActor, Vector3 pTo, float pSeconds, Color? pGhost = null)
        {
            if (!CanMove(pActor)) yield break;
            if (!Settings.Afterimages) pGhost = null;
            yield return CombatCore.Slide(pActor, Clamp(pTo), pSeconds, pGhost);
        }

        /// <summary>Vanishes and reappears elsewhere, leaving an afterimage.</summary>
        public void Blink(Vector3 pTo, string pFx = "vacuum")
        {
            if (!CanMove(A)) return;
            if (Settings.Afterimages) Fx.Ghost(A, Hot, 0.4f);
            F(pFx, P, 1.4f, 0f, 0f, false, 30f, pFx == "impact" || pFx == "shock");
            Place(A, pTo);
            F(pFx, P, 1.4f, 0f, 0f, false, 30f, pFx == "impact" || pFx == "shock");
            Snd("blink");
        }

        public static void Place(Actor pActor, Vector3 pTo)
        {
            if (!CanMove(pActor)) return;
            Vector3 p = Clamp(pTo);
            pActor.current_position = p;
            pActor.setCurrentTilePosition(CombatCore.TileAt(p));
        }

        public static void SetHeight(Actor pActor, float pHeight)
        {
            if (CanMove(pActor)) pActor.position_height = Mathf.Max(0f, pHeight);
        }

        /// <summary>Raises a unit into the air.</summary>
        public static IEnumerator Rise(Actor pActor, float pHeight, float pSeconds)
        {
            if (!CanMove(pActor)) yield break;
            float from = pActor.position_height, t = 0f;
            while (t < pSeconds && CombatCore.Alive(pActor))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                pActor.position_height = Mathf.Lerp(from, pHeight, Mathf.Sin(Mathf.Clamp01(t / pSeconds) * Mathf.PI * 0.5f));
                yield return null;
            }
        }

        /// <summary>Keeps a unit at its current height for a while (it would otherwise fall).</summary>
        public static IEnumerator Hang(Actor pActor, float pSeconds)
        {
            if (!CanMove(pActor)) yield break;
            float h = pActor.position_height, t = 0f;
            while (t < pSeconds && CombatCore.Alive(pActor))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                pActor.position_height = h + Mathf.Sin(t * 9f) * 0.06f;
                yield return null;
            }
            if (CombatCore.Alive(pActor)) pActor.position_height = h;
        }

        /// <summary>Drops a unit to the ground; fast drops are slams.</summary>
        public static IEnumerator Fall(Actor pActor, float pSeconds)
        {
            if (!CanMove(pActor)) yield break;
            float from = pActor.position_height, t = 0f;
            while (t < pSeconds && CombatCore.Alive(pActor))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                float k = Mathf.Clamp01(t / pSeconds);
                pActor.position_height = Mathf.Lerp(from, 0f, k * k);
                yield return null;
            }
            if (CombatCore.Alive(pActor)) pActor.position_height = 0f;
        }

        /// <summary>A jump: an arc to a point, peaking at a height.</summary>
        public static IEnumerator Arc(Actor pActor, Vector3 pTo, float pHeight, float pSeconds, Color? pGhost = null)
        {
            if (!CanMove(pActor)) yield break;
            if (!Settings.Afterimages) pGhost = null;
            Vector3 from = pActor.current_position;
            pTo = Clamp(pTo);
            float t = 0f, next_ghost = 0f;
            while (t < pSeconds && CombatCore.Alive(pActor))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                float k = Mathf.Clamp01(t / pSeconds);
                pActor.current_position = Vector3.Lerp(from, pTo, k);
                pActor.position_height = Mathf.Sin(k * Mathf.PI) * pHeight;
                if (pGhost.HasValue && t >= next_ghost)
                {
                    Fx.Ghost(pActor, pGhost.Value, 0.25f);
                    next_ghost = t + 0.04f;
                }
                yield return null;
            }
            if (!CombatCore.Alive(pActor)) yield break;
            pActor.position_height = 0f;
            pActor.setCurrentTilePosition(CombatCore.TileAt(pTo));
        }

        public IEnumerator Arc(Vector3 pTo, float pHeight, float pSeconds) => Arc(A, pTo, pHeight, pSeconds, Hot);

        /// <summary>Knocks a unit back along a direction, skidding and kicking up dust.</summary>
        public IEnumerator Knock(Actor pVictim, Vector3 pDir, float pDistance, float pSeconds)
        {
            if (!CanMove(pVictim)) yield break;
            Hold(pVictim, pSeconds + 0.3f);
            Vector3 from = pVictim.current_position;
            Vector3 to = Clamp(from + pDir.normalized * pDistance);
            float t = 0f, next_dust = 0f;
            while (t < pSeconds && CombatCore.Alive(pVictim))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / pSeconds), 2f);
                pVictim.current_position = Vector3.Lerp(from, to, k);
                if (t >= next_dust)
                {
                    F("dust", pVictim.current_position, 0.9f, 0f, 0f, false, 20f);
                    next_dust = t + 0.07f;
                }
                yield return null;
            }
            if (CombatCore.Alive(pVictim)) pVictim.setCurrentTilePosition(CombatCore.TileAt(to));
        }

        /// <summary>Starts a knockback without waiting for it.</summary>
        public void Push(Actor pVictim, Vector3 pDir, float pDistance, float pSeconds = 0.3f) =>
            ModClass.Instance.StartCoroutine(Knock(pVictim, pDir, pDistance, pSeconds));

        /// <summary>Pushes every enemy in a circle away from its centre.</summary>
        public void PushAll(Vector3 pCenter, float pRadius, float pDistance)
        {
            foreach (Actor u in Foes(pCenter, pRadius))
            {
                Vector3 d = (Vector3)u.current_position - pCenter;
                Push(u, d.sqrMagnitude < 0.01f ? (Vector3)Random.insideUnitCircle.normalized : d, pDistance);
            }
        }

        /// <summary>Pulls every enemy in a circle towards its centre.</summary>
        public void PullAll(Vector3 pCenter, float pRadius, float pSeconds = 0.35f)
        {
            foreach (Actor u in Foes(pCenter, pRadius))
            {
                Vector3 d = pCenter - (Vector3)u.current_position;
                if (d.magnitude > 0.5f) Push(u, d, d.magnitude - 0.4f, pSeconds);
            }
        }

        /// <summary>Throws a unit into the air, keeps it there, then drops it (without waiting).</summary>
        public void Launch(Actor pVictim, float pHeight, float pHang)
        {
            if (!CanMove(pVictim)) return;
            ModClass.Instance.StartCoroutine(LaunchRoutine(pVictim, pHeight, pHang));
        }

        private IEnumerator LaunchRoutine(Actor v, float pHeight, float pHang)
        {
            Hold(v, pHang + 0.8f);
            yield return Rise(v, pHeight, 0.18f);
            yield return Hang(v, pHang);
            yield return Fall(v, 0.22f);
            if (!CombatCore.Alive(v)) yield break;
            Dust(v.current_position, 1.2f);
            Snd("land", v.current_position);
        }

        // ---------------------------------------------------------------- impact and terrain

        /// <summary>
        /// The weight of a blow. Strength 0..1: a shake for everything, a camera punch from 0.5, and at
        /// Level III (or strength 1) the world freezes for a moment on impact.
        /// </summary>
        public void Impact(Vector3 pPos, float pStrength)
        {
            if (!Director.OnScreen(pPos)) return;
            float s = Mathf.Clamp01(pStrength) * (Lv >= 3 ? 1.25f : 1f);
            World.world.startShake(0.15f + 0.35f * s, 0.01f, 1f + 4f * s, true, true);
            if (s >= 0.5f) Director.Punch(1f - 0.1f * s, 0.25f);
            if (Lv >= 3 && s >= 0.6f) Director.HitStop(0.06f + 0.1f * s);
            if (s >= 0.95f) Director.Impact(pPos, Hot, s);
        }

        public static void Shake(float pStrength) =>
            World.world.startShake(0.15f + 0.35f * pStrength, 0.01f, 1f + 4f * pStrength, true, true);

        /// <summary>A scorched crater that lingers (Level II+ blows leave bigger ones).</summary>
        public void Crater(Vector3 pPos, float pRadius)
        {
            if (!Settings.Craters || !Director.OnScreen(pPos)) return;
            Fx.Play("crater", pPos, Mathf.Clamp(pRadius * 2f, 1.5f, 14f), new Color(0.16f, 0.12f, 0.1f, 0.85f), 0f,
                6f, false, 1f, "EffectsBack");
            F("debris", pPos, pRadius * 1.4f, 0f, 0f, false, 16f);
        }

        /// <summary>A crack torn through the ground between two points.</summary>
        public void Fissure(Vector3 pFrom, Vector3 pTo, float pWidth = 1f)
        {
            if (!Settings.Craters || !Director.OnScreen(pFrom)) return;
            Fx.Instance fx = Fx.Line("arts/fissure", pFrom, pTo, pWidth, Color.white, 7f, false);
            if (fx != null) fx.Renderer.sortingLayerName = "EffectsBack";
        }

        /// <summary>Sets the ground alight (only with destructive effects on).</summary>
        public static void Scorch(Vector3 pPos, float pRadius, int pFires = 3)
        {
            if (!Settings.DestructiveEffects) return;
            for (int i = 0; i < pFires; i++)
            {
                WorldTile tile = CombatCore.TileAt(pPos + Rand(pRadius));
                if (tile != null) ActionLibrary.burnTile(null, null, tile);
            }
        }
    }
}
