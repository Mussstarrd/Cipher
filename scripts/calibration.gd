extends Control
## Calibration screen: a synthesized metronome loops at the reference BPM
## with a visual pulse; the player taps along. We take the median offset
## between tap time and true beat time over 16 taps, discard outliers, and
## store the result persistently. The offset is applied to all temporal
## judgment in gameplay.

const REF_BPM := 90.0
const TAPS_NEEDED := 8
const OUTLIER_MS := 60.0
## Taps further than 40% of a beat from any beat are ignored outright.
const IGNORE_FRACTION := 0.4

var _player: AudioStreamPlayer
var _deltas: Array[float] = []
var _done := false

var _progress: Label
var _result: Label


func _ready() -> void:
	# Let taps fall through to _unhandled_input instead of being consumed
	# by this full-screen Control in the GUI phase.
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_player = AudioStreamPlayer.new()
	_player.stream = ClickSynth.click_track(REF_BPM)
	add_child(_player)
	# The click track is linear (not looped); restart it if it runs out.
	_player.finished.connect(_restart)
	_build_ui()
	_restart()


func _exit_tree() -> void:
	_player.stop()
	SongClock.stop()


func _build_ui() -> void:
	var title := Label.new()
	title.text = "CALIBRATION"
	title.anchor_left = 0.5
	title.anchor_right = 0.5
	title.offset_left = -300.0
	title.offset_right = 300.0
	title.offset_top = 30.0
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 44)
	add_child(title)

	var hint := Label.new()
	hint.text = "Tap anywhere in time with the CLICK SOUND (not the circle). %d taps — about 6 seconds.\nIf the circle's pop doesn't match the click: open DBG (top right) and nudge VIS +/- until it does." % TAPS_NEEDED
	hint.anchor_left = 0.5
	hint.anchor_right = 0.5
	hint.offset_left = -400.0
	hint.offset_right = 400.0
	hint.offset_top = 92.0
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hint.add_theme_font_size_override("font_size", 24)
	add_child(hint)

	_progress = Label.new()
	_progress.anchor_left = 0.5
	_progress.anchor_right = 0.5
	_progress.offset_left = -200.0
	_progress.offset_right = 200.0
	_progress.offset_top = 540.0
	_progress.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_progress.add_theme_font_size_override("font_size", 28)
	add_child(_progress)

	_result = Label.new()
	_result.anchor_left = 0.5
	_result.anchor_right = 0.5
	_result.offset_left = -400.0
	_result.offset_right = 400.0
	_result.offset_top = 590.0
	_result.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result.add_theme_font_size_override("font_size", 24)
	_result.add_theme_color_override("font_color", Color(0.5, 1.0, 0.6))
	add_child(_result)

	var redo := Button.new()
	redo.text = "REDO"
	redo.offset_left = 12.0
	redo.offset_right = 130.0
	redo.offset_top = 12.0
	redo.offset_bottom = 56.0
	redo.pressed.connect(_restart)
	add_child(redo)

	var back := Button.new()
	back.text = "MENU"
	back.anchor_top = 1.0
	back.anchor_bottom = 1.0
	back.offset_left = 12.0
	back.offset_right = 130.0
	back.offset_top = -56.0
	back.offset_bottom = -12.0
	back.pressed.connect(
		func() -> void:
			get_tree().change_scene_to_file("res://scenes/main_menu.tscn"))
	add_child(back)


func _restart() -> void:
	_deltas.clear()
	_done = false
	_result.text = ""
	_player.stop()
	_player.play()
	SongClock.configure(REF_BPM, 0.0, 50.0)
	SongClock.start(_player)
	_update_progress()


func _unhandled_input(event: InputEvent) -> void:
	if not event.is_action_pressed("bounce") or _done or not SongClock.running:
		return
	Input.vibrate_handheld(15)
	var b := SongClock.current_beat()
	var delta_ms := (b - roundf(b)) * SongClock.beat_duration() * 1000.0
	if absf(delta_ms) > SongClock.beat_duration() * 1000.0 * IGNORE_FRACTION:
		return
	_deltas.append(delta_ms)
	_update_progress()
	if _deltas.size() >= TAPS_NEEDED:
		_finish()


func _finish() -> void:
	var m := _median(_deltas)
	var kept: Array[float] = []
	for d in _deltas:
		if absf(d - m) <= OUTLIER_MS:
			kept.append(d)
	var result := _median(kept) if kept.size() >= 3 else m
	Settings.calibration_offset_ms = result
	Settings.has_calibration = true
	Settings.save_settings()
	_done = true
	_result.text = "Saved offset: %+.0f ms (positive = you tap late)\nRedo any time from the main menu." % result


func _update_progress() -> void:
	_progress.text = "Taps: %d / %d" % [_deltas.size(), TAPS_NEEDED]


static func _median(values: Array[float]) -> float:
	if values.is_empty():
		return 0.0
	var s := values.duplicate()
	s.sort()
	var n := s.size()
	if n % 2 == 1:
		return s[n / 2]
	return (s[n / 2 - 1] + s[n / 2]) * 0.5


func _process(_delta: float) -> void:
	queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.06, 0.07, 0.10))
	if not SongClock.running:
		return
	# The pulse pops exactly on the click. The A/V offset (DBG overlay,
	# VIS +/- buttons) shifts this visual against the audio for devices
	# that misreport output latency — tune it here until pop == click.
	var beat := SongClock.current_beat() \
		- Settings.av_offset_ms / 1000.0 / SongClock.beat_duration()
	var frac := fposmod(beat, 1.0)
	var center := size * 0.5 + Vector2(0.0, -20.0)
	var accent := fposmod(beat, 4.0) < 1.0
	var col := Color(0.95, 0.72, 0.28) if accent else Color(0.5, 0.55, 0.75)
	var pop := exp(-frac * 7.0)
	draw_circle(center, 46.0 + 34.0 * pop, Color(col, 0.20 + 0.5 * pop))
	draw_circle(center, 40.0 + 14.0 * pop, col)
	# thin approach ring so the next pop is predictable
	draw_arc(center, 54.0 + 120.0 * (1.0 - frac), 0.0, TAU, 48,
		Color(col, 0.35), 2.5)
