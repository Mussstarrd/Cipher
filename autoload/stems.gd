extends Node
## Stems (autoload) — owns every AudioStreamPlayer so all stems of a track
## start in the same frame and stay sample-locked.
##
## Player hit feedback is UNMUTE-BASED, never a reactive one-shot: the
## player_drums stem plays in perfect sync at all times, muted at -80 dB.
## A successful bounce unmutes it briefly, so the player hears a perfectly
## on-grid hit regardless of touch latency. Escalation stems work the same
## way, gated by chain level.

signal track_finished

const MUTE_DB := -80.0

var instrumental: AudioStreamPlayer = null
var drums: AudioStreamPlayer = null
var escalation: Array[AudioStreamPlayer] = []

var _drums_open_until_beat := -INF


## stem_paths matches the chart schema:
## {"instrumental": path, "player_drums": path, "escalation": [paths]}
## If bpm > 0 and the instrumental turns out to be the silent placeholder,
## it is replaced with a synthesized click track so the beat is audible
## until real stems are dropped in.
func load_track(stem_paths: Dictionary, bpm := 0.0, fallback_beats := 0) -> bool:
	clear()
	instrumental = _make_player(str(stem_paths.get("instrumental", "")), 0.0)
	if instrumental == null:
		push_error("Stems: could not load instrumental stem")
		return false
	if bpm > 0.0 and fallback_beats > 0 and _is_silent_wav(instrumental.stream):
		instrumental.stream = ClickSynth.click_track(bpm, fallback_beats)
	instrumental.finished.connect(func() -> void: track_finished.emit())
	drums = _make_player(str(stem_paths.get("player_drums", "")), MUTE_DB)
	for p in stem_paths.get("escalation", []):
		var esc := _make_player(str(p), MUTE_DB)
		if esc != null:
			escalation.append(esc)
	return true


func clear() -> void:
	stop_all()
	for p in _players():
		p.queue_free()
	instrumental = null
	drums = null
	escalation.clear()
	_drums_open_until_beat = -INF


func play_all(from := 0.0) -> void:
	_drums_open_until_beat = -INF
	if drums != null:
		drums.volume_db = MUTE_DB
	for p in _players():
		p.play(from)


func stop_all() -> void:
	for p in _players():
		p.stop()


## Unmute the player-drums stem until the given song beat.
func open_drums_until(beat: float) -> void:
	if drums == null:
		return
	drums.volume_db = 0.0
	_drums_open_until_beat = maxf(_drums_open_until_beat, beat)


## Unmute the first `level` escalation stems, mute the rest.
func set_escalation_level(level: int) -> void:
	for i in escalation.size():
		escalation[i].volume_db = 0.0 if i < level else MUTE_DB


func _process(_delta: float) -> void:
	if drums == null or not SongClock.running:
		return
	if _drums_open_until_beat > -INF \
			and SongClock.current_beat() >= _drums_open_until_beat:
		drums.volume_db = MUTE_DB
		_drums_open_until_beat = -INF


func _players() -> Array[AudioStreamPlayer]:
	var out: Array[AudioStreamPlayer] = []
	if instrumental != null:
		out.append(instrumental)
	if drums != null:
		out.append(drums)
	out.append_array(escalation)
	return out


static func _is_silent_wav(stream: AudioStream) -> bool:
	if not stream is AudioStreamWAV:
		return false
	var d: PackedByteArray = (stream as AudioStreamWAV).data
	var i := 0
	while i < d.size():
		if d[i] != 0:
			return false
		i += 1024
	return d.size() > 0


func _make_player(path: String, vol_db: float) -> AudioStreamPlayer:
	if path.is_empty() or not ResourceLoader.exists(path):
		return null
	var stream: AudioStream = load(path)
	if stream == null:
		return null
	var p := AudioStreamPlayer.new()
	p.stream = stream
	p.volume_db = vol_db
	add_child(p)
	return p
