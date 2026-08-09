class_name ClickSynth
extends RefCounted
## Synthesises a looping metronome WAV at runtime for the calibration
## screen. Because the clicks are baked into a stream that loops sample-
## accurately, the SongClock can derive time from its playback position
## exactly like a real track — no scheduled one-shots involved.


static func metronome_loop(bpm: float, beats := 4, sample_rate := 44100) -> AudioStreamWAV:
	var spb := 60.0 / bpm
	var frames := int(roundf(beats * spb * sample_rate))
	var data := PackedByteArray()
	data.resize(frames * 2)  # 16-bit mono, zero-initialised (silence)

	var click_frames := int(0.03 * sample_rate)
	for b in beats:
		var start := int(roundf(b * spb * sample_rate))
		var freq := 1320.0 if b == 0 else 880.0  # accent the downbeat
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
	wav.loop_mode = AudioStreamWAV.LOOP_FORWARD
	wav.loop_begin = 0
	wav.loop_end = frames
	return wav
