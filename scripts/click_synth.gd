class_name ClickSynth
extends RefCounted
## Synthesises a metronome click track at runtime for the calibration
## screen. The clicks are baked into one long linear stream, so the
## SongClock derives time from its playback position exactly like a real
## track — no scheduled one-shots, and no loop points (sample loops in
## runtime-built streams are avoided as a crash-safety measure on Android).


static func click_track(bpm: float, beats := 96, sample_rate := 44100) -> AudioStreamWAV:
	var spb := 60.0 / bpm
	var frames := int(roundf(beats * spb * sample_rate))
	var data := PackedByteArray()
	data.resize(frames * 2)  # 16-bit mono, zero-initialised (silence)

	var click_frames := int(0.03 * sample_rate)
	for b in beats:
		var start := int(roundf(b * spb * sample_rate))
		var freq := 1320.0 if b % 4 == 0 else 880.0  # accent the downbeat
		for i in click_frames:
			var idx := start + i
			if idx >= frames:
				break
			var env := exp(-6.0 * float(i) / float(click_frames))
			var sample := int(sin(TAU * freq * float(i) / float(sample_rate)) \
				* env * 0.8 * 32767.0)
			data.encode_s16(idx * 2, sample)

	var wav := AudioStreamWAV.new()
	wav.data = data
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = sample_rate
	wav.stereo = false
	return wav
