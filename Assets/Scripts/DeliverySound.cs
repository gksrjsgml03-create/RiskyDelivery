using System;
using UnityEngine;

namespace RiskyDelivery
{
    // Original synthesized sounds keep the small game self-contained and require no external assets.
    public sealed class DeliverySound
    {
        private readonly AudioSource effects, ui, rolling;
        private readonly AudioClip click, impact, delivered, failed, campaign, roll;
        private bool enabled;

        public DeliverySound(GameObject owner, bool soundEnabled)
        {
            effects = owner.AddComponent<AudioSource>();
            ui = owner.AddComponent<AudioSource>();
            rolling = owner.AddComponent<AudioSource>();
            foreach (var source in new[] { effects, ui, rolling })
            {
                source.playOnAwake = false;
                source.spatialBlend = 0;
            }
            ui.ignoreListenerPause = true;
            click = Tone("Menu click", 0.07f, t => Mathf.Sin(t * 2 * Mathf.PI * 650) * 0.08f);
            impact = Tone("Cargo impact", 0.18f, t => (Mathf.Sin(t * 2 * Mathf.PI * 85) + Mathf.Sin(t * 2 * Mathf.PI * 137)) * 0.12f * Mathf.Exp(-t * 14));
            float[] deliveryNotes = { 523.25f, 659.25f, 783.99f };
            float[] campaignNotes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            delivered = Tone("Delivery chime", 0.48f, t => Note(t, 0.16f, deliveryNotes));
            failed = Tone("Delivery failed", 0.5f, t => Mathf.Sin(2 * Mathf.PI * (250 * t - 150 * t * t)) * 0.14f);
            campaign = Tone("Campaign complete", 0.8f, t => Note(t, 0.2f, campaignNotes));
            roll = Tone("Courier footsteps", 1, t => {
                float step = Mathf.Repeat(t, 0.5f);
                return Mathf.Sin(step * 2 * Mathf.PI * 95) * Mathf.Exp(-step * 45) * Mathf.Clamp01(step / 0.008f) * 0.3f;
            }, false);
            rolling.clip = roll;
            rolling.loop = true;
            SetEnabled(soundEnabled);
        }

        private static float Note(float time, float duration, float[] notes)
        {
            int index = Mathf.Min((int)(time / duration), notes.Length - 1);
            float local = time - index * duration;
            float envelope = Mathf.Clamp01(local / 0.008f) * Mathf.Clamp01((duration - local) / 0.025f);
            return Mathf.Sin(local * 2 * Mathf.PI * notes[index]) * 0.16f * envelope;
        }

        private static AudioClip Tone(string name, float duration, Func<float, float> sample, bool fade = true)
        {
            const int rate = 22050;
            var data = new float[Mathf.CeilToInt(duration * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = fade ? Mathf.Min(1, t / 0.008f) * Mathf.Clamp01((duration - t) / 0.06f) : 1;
                data[i] = sample(t) * envelope;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
            effects.mute = ui.mute = rolling.mute = !value;
            effects.volume = 0.7f;
            ui.volume = 0.5f;
            if (!value) rolling.Stop();
        }

        public void Tick(float speed, bool driving)
        {
            bool moving = enabled && driving && speed > 0.15f;
            if (!moving) { if (rolling.isPlaying) rolling.Stop(); return; }
            rolling.volume = Mathf.Clamp01(speed / 8) * 0.3f;
            rolling.pitch = 0.8f + Mathf.Clamp01(speed / 8) * 0.4f;
            if (!rolling.isPlaying) rolling.Play();
        }

        public void Click() { if (enabled) ui.PlayOneShot(click); }
        public void Impact() { if (enabled) effects.PlayOneShot(impact); }
        public void Failed() { if (enabled) { effects.Stop(); effects.PlayOneShot(failed); } }
        public void Delivered(bool allDone) { if (enabled) { effects.Stop(); effects.PlayOneShot(allDone ? campaign : delivered); } }
        public void Reset() { effects.Stop(); rolling.Stop(); }

        public void Dispose()
        {
            foreach (var clip in new[] { click, impact, delivered, failed, campaign, roll }) UnityEngine.Object.Destroy(clip);
        }
    }
}
