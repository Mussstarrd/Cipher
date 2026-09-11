#nullable enable
using System;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// The implant is decrypted progressively, and nothing is ever dead before its bar is empty.
    /// ADR-008, second pass.
    ///
    /// THE FIRST VERSION HAD A FREE-DEATH STATE AND THE OWNER FOUND IT IN ONE SITTING:
    ///
    ///   "just because a zombie gets hit doesn't inherently mean they slow down all the way to
    ///    death ... I want them to need to take multiple shots or pulses to be able to accelerate
    ///    their health bars going down. just tagging them and being able to run in circles is an
    ///    easy way to beat the game."
    ///
    /// He was describing an exploit and he was right. Version one broke the chip the instant
    /// integrity hit zero and then guaranteed death 2.5 seconds later: the player was PAID at the
    /// break, the turrets stopped shooting at the body, and its threat decayed to nothing. So the
    /// optimal play was to land a killing shot on everything and walk away. The wave was already
    /// over; the two and a half seconds were a formality.
    ///
    /// The model now has one number the player can see and one number they cannot:
    ///
    ///   INTEGRITY  the health bar. Only reaching zero kills, and only then is anyone paid.
    ///   DRAIN      how fast the malware is eating it, in integrity per second.
    ///
    /// A pulse does two things: direct damage, and it adds to the drain. **One hit is a death
    /// sentence that takes about forty seconds to arrive** (`SoloDecryptSeconds`), which is far too
    /// slow to save anyone from a wave -- tag forty people once each and all forty reach the truck
    /// first. Hits stack, so focused fire kills in seconds. That is the whole design: the player
    /// chooses between spreading the infection and finishing the job, and only one of those works.
    ///
    /// Speed and threat decay only across the LAST stretch of the bar (`FrailtyBelow`), so a body
    /// that has been tagged once and is at ninety per cent is a full-strength attacker. That is
    /// the owner's first sentence, made mechanical.
    ///
    /// No RNG, as everywhere in the core.
    /// </summary>
    public sealed partial class AgentWorld
    {
        private float[] _drain = Array.Empty<float>();
        private float[] _maxHealth = Array.Empty<float>();
        private long _brokenSappers;
        private long _brokenSpitters;

        /// <summary>
        /// Chips broken per archetype, cumulative.
        ///
        /// The game layer used to infer Sapper and Spitter deaths by watching the living count
        /// fall, because the sim raises no per-archetype death event. Counting at the kill is
        /// exact, so the inference could be deleted rather than patched.
        /// </summary>
        public long BrokenOf(Archetype archetype) => archetype switch
        {
            Archetype.Sapper => _brokenSappers,
            Archetype.Spitter => _brokenSpitters,
            _ => TotalBroken - _brokenSappers - _brokenSpitters,
        };

        private void NoteBroken(Archetype archetype)
        {
            if (archetype == Archetype.Sapper) _brokenSappers++;
            else if (archetype == Archetype.Spitter) _brokenSpitters++;
        }

        /// <summary>
        /// True when a gun should look past this body: it is already due to fall within
        /// <see cref="SimConfig.SpokenForSeconds"/> without anyone spending another round on it.
        ///
        /// Version one asked "is it infected", which was fine when infection meant death in 2.5
        /// seconds. Under the decrypt model nearly everyone on the field is infected, so that
        /// question would make the whole crowd invisible to every turret in the game.
        /// </summary>
        private bool IsSpokenFor(int id) => SecondsToDie(id) <= _config.SpokenForSeconds;

        /// <summary>Chips finished off since the world was created. Tracks <see cref="TotalKills"/>.</summary>
        public long TotalBroken { get; private set; }

        /// <summary>Bodies with malware in them that are still on their feet.</summary>
        public int FailingCount { get; private set; }

        /// <summary>
        /// True once anything has hit this agent -- its implant is being decrypted and it will die
        /// eventually even if never touched again. It is NOT a promise that death is imminent, and
        /// nothing may treat it as one.
        /// </summary>
        public bool IsFailing(int id) => id >= 0 && id < Count && _alive[id] && _drain[id] > 0f;

        /// <summary>What is left of the bar, 1 to 0. This is the thing to draw over their head.</summary>
        public float Integrity01(int id)
        {
            if (id < 0 || id >= Count || !_alive[id]) return 0f;
            float max = _maxHealth[id];
            return max <= 0f ? 0f : Math.Clamp(_health[id] / max, 0f, 1f);
        }

        /// <summary>
        /// Seconds until this one falls over on its own, or <see cref="float.PositiveInfinity"/>
        /// if nothing has touched it. What a gun uses to decide the body is already spoken for.
        /// </summary>
        public float SecondsToDie(int id)
        {
            if (id < 0 || id >= Count || !_alive[id]) return 0f;
            if (_drain[id] <= 0f) return float.PositiveInfinity;
            return Math.Max(0f, _health[id]) / _drain[id];
        }

        /// <summary>
        /// How badly the decryption has taken hold, 0 to 1, over the last stretch of the bar only.
        /// A body at eighty per cent is not frail; a body at five per cent barely works.
        /// </summary>
        private float Frailty(int id)
        {
            float integrity = Integrity01(id);
            // A Collector holds its speed and its swing almost to zero: no comfortable stretch at
            // the end where it is still standing but has stopped being dangerous.
            float floor = (Archetype)_archetype[id] == Archetype.Collector
                ? _config.FrailtyBelow * 0.25f
                : _config.FrailtyBelow;
            if (floor <= 0f || integrity >= floor) return 0f;
            return 1f - integrity / floor;
        }

        /// <summary>
        /// What this agent's attacks are worth right now. Full strength until the bar is nearly
        /// out, then falling away. Callers multiply rather than checking a state.
        /// </summary>
        public float ThreatScale(int id) => 1f - Frailty(id) * (1f - _config.FailThreatFloor);

        /// <summary>
        /// What this agent's legs are worth right now. Floored above zero: a body that stops dead
        /// reads as a freeze, and the point is that it keeps coming.
        /// </summary>
        public float SpeedScale(int id) => 1f - Frailty(id) * (1f - _config.FailSpeedFloor);

        /// <summary>
        /// Puts malware into an implant: <paramref name="damage"/> off the bar now, and a lasting
        /// drain that keeps eating it. Returns true only when THIS call emptied the bar, which is
        /// the one moment the player is paid.
        ///
        /// The drain from a single hit is deliberately feeble. It has to be a real death sentence
        /// -- the fiction says the implant is finished the moment the payload lands -- and it has
        /// to be useless as a tactic.
        /// </summary>
        private bool Infect(int id, float damage)
        {
            if (_health[id] <= 0f) return false;

            float perHit = _maxHealth[id] > 0f
                ? _maxHealth[id] / Math.Max(0.01f, _config.SoloDecryptSeconds)
                : 0f;
            // ADR-011: lab hardware resists. The arsenal still works on a Collector, it just works
            // badly, which is why the player is not simply disarmed by one.
            perHit *= DrainResistance(id);
            if (_drain[id] <= 0f) FailingCount++;
            _drain[id] += perHit;

            _health[id] -= damage;
            if (_health[id] > 0f) return false;

            Retire(id);
            return true;
        }

        /// <summary>
        /// The bar reached zero. Pays the player and takes the body off the field.
        ///
        /// **Both routes to an empty bar pay**: the pulse that finishes it, and the slow decrypt
        /// finishing it forty seconds later with nobody watching. Withholding payment for the slow
        /// one was tempting -- it would punish spraying -- but it is the wrong lever and it lies to
        /// the HUD. The player DID kill that one; they killed it with one shot and then spent forty
        /// seconds not being able to use the cash. The forty seconds IS the punishment, which is
        /// exactly what the owner asked for. An under-counting kill tally would just look broken.
        /// </summary>
        private void Retire(int id)
        {
            _health[id] = 0f;
            if (_drain[id] > 0f) { _drain[id] = 0f; FailingCount--; }
            _alive[id] = false;
            AliveCount--;
            _hashDirty = true;

            TotalBroken++;
            TotalKills++;
            NoteBroken((Archetype)_archetype[id]);
        }

        /// <summary>
        /// Eats the bar at each agent's own drain rate and retires the ones that reach zero. Called
        /// once per tick AFTER movement, so a body gets its last step before it falls.
        /// </summary>
        private void StepFailing(float dt)
        {
            if (FailingCount == 0) return;

            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i] || _drain[i] <= 0f) continue;

                _health[i] -= _drain[i] * dt;
                if (_health[i] > 0f) continue;

                Retire(i);
            }
        }
    }
}
