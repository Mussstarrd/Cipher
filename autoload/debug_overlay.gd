extends CanvasLayer
## DebugOverlay (autoload) — toggleable diagnostics (DBG button or F3).
## Shows clock jitter (raw vs smoothed), reported output latency, the last
## 10 judgment deltas, current beat, chain, and FPS. This is how we decide
## whether deeper audio work is needed on a given device.

const MAX_JUDGMENTS := 10
const REFRESH_SEC := 0.1

var chain := 0

var _judgments: Array[float] = []
var _label: Label
var _shown := false
var _accum := 0.0


func _ready() -> void:
	layer = 100
	_label = Label.new()
	_label.position = Vector2(12, 12)
	_label.add_theme_font_size_override("font_size", 18)
	_label.add_theme_color_override("font_color", Color(0.45, 1.0, 0.55))
	_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	_label.add_theme_constant_override("shadow_offset_x", 1)
	_label.add_theme_constant_override("shadow_offset_y", 1)
	_label.visible = false
	add_child(_label)

	var btn := Button.new()
	btn.text = "DBG"
	btn.modulate.a = 0.55
	btn.anchor_left = 1.0
	btn.anchor_right = 1.0
	btn.offset_left = -76.0
	btn.offset_right = -12.0
	btn.offset_top = 10.0
	btn.offset_bottom = 48.0
	btn.pressed.connect(toggle)
	add_child(btn)

	# A/V sync nudge: shifts cue visuals vs the audio (persisted). Only
	# shown while the overlay is open.
	_av_minus = _av_button("VIS -10ms", -66.0, -10.0)
	_av_plus = _av_button("VIS +10ms", -114.0, 10.0)


func _av_button(text: String, top_offset: float, delta: float) -> Button:
	var b := Button.new()
	b.text = text
	b.modulate.a = 0.7
	b.anchor_left = 1.0
	b.anchor_right = 1.0
	b.anchor_top = 1.0
	b.anchor_bottom = 1.0
	b.offset_left = -150.0
	b.offset_right = -12.0
	b.offset_top = top_offset
	b.offset_bottom = top_offset + 42.0
	b.visible = false
	b.pressed.connect(func() -> void:
		Settings.av_offset_ms = clampf(Settings.av_offset_ms + delta, -250.0, 250.0)
		Settings.save_settings())
	add_child(b)
	return b


var _av_minus: Button
var _av_plus: Button


func toggle() -> void:
	_shown = not _shown
	_label.visible = _shown
	_av_minus.visible = _shown
	_av_plus.visible = _shown


func push_judgment(delta_ms: float) -> void:
	_judgments.append(delta_ms)
	while _judgments.size() > MAX_JUDGMENTS:
		_judgments.pop_front()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("debug_toggle"):
		toggle()


func _process(delta: float) -> void:
	if not _shown:
		return
	_accum += delta
	if _accum < REFRESH_SEC:
		return
	_accum = 0.0
	var j := ""
	for m in _judgments:
		j += "%+.0f " % m
	_label.text = (
		"clock jitter (raw-smoothed): %+.1f ms\n" % SongClock.jitter_ms()
		+ "output latency: %.1f ms\n" % SongClock.output_latency_ms()
		+ "beat: %.2f\n" % SongClock.current_beat()
		+ "chain: %d\n" % chain
		+ "fps: %d\n" % Engine.get_frames_per_second()
		+ "cal offset: %+.0f ms   av offset: %+.0f ms\n" \
			% [Settings.calibration_offset_ms, Settings.av_offset_ms]
		+ "judgments (ms, +late): " + j
	)
