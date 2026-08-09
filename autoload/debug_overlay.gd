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


func toggle() -> void:
	_shown = not _shown
	_label.visible = _shown


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
		+ "cal offset: %+.0f ms\n" % Settings.calibration_offset_ms
		+ "judgments (ms, +late): " + j
	)
