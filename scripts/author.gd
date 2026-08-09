extends Control
## Chart authoring mode. The track loops with a scrolling beat grid; tapping
## places an enemy at the grid slot nearest the playhead (respecting the
## current difficulty resolution and swing). Delete mode removes the event
## nearest the tapped x position. Undo reverses the last edit. Save writes
## the chart JSON to user://, which the runner then prefers over the
## shipped res:// chart.

const CHART_SAVE_PATH := "user://charts/demo.chart.json"
const RES_CHART_PATH := "res://charts/demo.chart.json"
const PLAYHEAD_X := 420.0
const PX_PER_BEAT := 170.0
const BAND_TOP := 170.0
const BAND_BOTTOM := 560.0

var chart: Chart
var undo_stack: Array[Dictionary] = []
var delete_mode := false

var _status: Label
var _flash_label: Label
var _mode_btn: Button
var _grid_btn: Button


func _ready() -> void:
	# Let taps fall through to _unhandled_input instead of being consumed
	# by this full-screen Control in the GUI phase.
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	chart = _load_chart()
	_build_ui()
	if chart == null:
		_flash("Could not load chart")
		return
	SongClock.configure(chart.bpm, chart.offset_ms, chart.swing_percent)
	if Stems.load_track(chart.stems):
		Stems.track_finished.connect(_on_track_finished)
		_restart_playback()
	else:
		_flash("Could not load audio stems")


func _exit_tree() -> void:
	Stems.stop_all()
	SongClock.stop()


func _load_chart() -> Chart:
	if FileAccess.file_exists(CHART_SAVE_PATH):
		var c := Chart.load_from_file(CHART_SAVE_PATH)
		if c != null:
			return c
	return Chart.load_from_file(RES_CHART_PATH)


func _restart_playback() -> void:
	Stems.stop_all()
	Stems.play_all()
	SongClock.start(Stems.instrumental)


func _on_track_finished() -> void:
	_restart_playback()  # loop for continuous authoring


func _build_ui() -> void:
	var bar := HBoxContainer.new()
	bar.offset_left = 12.0
	bar.offset_top = 12.0
	bar.add_theme_constant_override("separation", 10)
	add_child(bar)

	bar.add_child(_bar_button("MENU",
		func() -> void:
			get_tree().change_scene_to_file("res://scenes/main_menu.tscn")))
	bar.add_child(_bar_button("RESTART", _restart_playback))

	_mode_btn = _bar_button("MODE: PLACE", _toggle_mode)
	bar.add_child(_mode_btn)

	_grid_btn = _bar_button("", _cycle_grid)
	_refresh_grid_label()
	bar.add_child(_grid_btn)

	bar.add_child(_bar_button("UNDO", _undo))
	bar.add_child(_bar_button("SAVE", _save))

	_status = Label.new()
	_status.anchor_top = 1.0
	_status.anchor_bottom = 1.0
	_status.offset_left = 12.0
	_status.offset_top = -76.0
	_status.offset_right = 900.0
	_status.add_theme_font_size_override("font_size", 20)
	add_child(_status)

	_flash_label = Label.new()
	_flash_label.anchor_top = 1.0
	_flash_label.anchor_bottom = 1.0
	_flash_label.offset_left = 12.0
	_flash_label.offset_top = -44.0
	_flash_label.offset_right = 1100.0
	_flash_label.add_theme_font_size_override("font_size", 20)
	_flash_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.4))
	add_child(_flash_label)


func _bar_button(text: String, on_pressed: Callable) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0.0, 52.0)
	b.add_theme_font_size_override("font_size", 20)
	b.pressed.connect(on_pressed)
	return b


func _toggle_mode() -> void:
	delete_mode = not delete_mode
	_mode_btn.text = "MODE: DELETE" if delete_mode else "MODE: PLACE"


func _cycle_grid() -> void:
	Settings.cycle_difficulty()
	_refresh_grid_label()


func _refresh_grid_label() -> void:
	var names := {"easy": "1/4", "medium": "1/8", "hard": "1/16"}
	_grid_btn.text = "GRID: %s" % names[Settings.difficulty]


func _unhandled_input(event: InputEvent) -> void:
	if not event.is_action_pressed("bounce"):
		return
	if chart == null or not SongClock.running:
		return
	Input.vibrate_handheld(15)
	if delete_mode:
		_delete_at_x(get_viewport().get_mouse_position().x)
	else:
		_place_at_playhead()


func _place_at_playhead() -> void:
	# The author taps in rhythm, so their calibration offset applies here too.
	var t := SongClock.time() - Settings.calibration_offset_ms / 1000.0
	var g := SongClock.nearest_grid(t, Settings.grid_step())
	var b: float = float(g["beat"])
	if b < 0.0:
		return
	for e in chart.events:
		if absf(float(e["beat"]) - b) < 0.001:
			_flash("Beat %.2f already has an event" % b)
			return
	var ev := {"beat": b, "type": "enemy",
		"nudge_ms": 0, "lane": 0, "duration_beats": 0.0}
	chart.events.append(ev)
	chart.sort_events()
	undo_stack.append({"op": "add", "ev": ev})
	_flash("Placed at beat %.2f" % b)


func _delete_at_x(x: float) -> void:
	var px_per_sec := PX_PER_BEAT / SongClock.beat_duration()
	var t := SongClock.time() + (x - PLAYHEAD_X) / px_per_sec
	var best: Variant = null
	var best_dt := 0.35 * SongClock.beat_duration()
	for e in chart.events:
		var et := SongClock.grid_beat_to_time(float(e["beat"]))
		var dt := absf(et - t)
		if dt < best_dt:
			best_dt = dt
			best = e
	if best == null:
		_flash("No event near tap")
		return
	chart.events.erase(best)
	undo_stack.append({"op": "del", "ev": best})
	_flash("Deleted event at beat %.2f" % float(best["beat"]))


func _undo() -> void:
	if undo_stack.is_empty():
		_flash("Nothing to undo")
		return
	var a: Dictionary = undo_stack.pop_back()
	if a["op"] == "add":
		chart.events.erase(a["ev"])
	else:
		chart.events.append(a["ev"])
		chart.sort_events()
	_flash("Undo")


func _save() -> void:
	if chart.save(CHART_SAVE_PATH):
		_flash("Saved %d events to %s" % [chart.events.size(), CHART_SAVE_PATH])
	else:
		_flash("SAVE FAILED")


func _flash(msg: String) -> void:
	_flash_label.text = msg


func _process(_delta: float) -> void:
	queue_redraw()
	if chart == null or not SongClock.running:
		return
	_status.text = "beat %.2f  |  %d events  |  %s  |  tap: %s" % [
		SongClock.current_beat(), chart.events.size(),
		_grid_btn.text.to_lower(),
		"delete nearest" if delete_mode else "place at playhead"]


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.06, 0.07, 0.10))
	if chart == null or not SongClock.running:
		return
	var px_per_sec := PX_PER_BEAT / SongClock.beat_duration()
	var now := SongClock.time()
	var t1 := now + (size.x - PLAYHEAD_X) / px_per_sec
	var step := Settings.grid_step()
	var lane_y := (BAND_TOP + BAND_BOTTOM) / 2.0

	# Grid lines at the current resolution, drawn at their swung times.
	var t0 := now - PLAYHEAD_X / px_per_sec
	var b := floorf(SongClock.linear_beat_at(t0) / step) * step - step
	var guard := 0
	while guard < 400:
		guard += 1
		var bt := SongClock.grid_beat_to_time(b)
		if bt > t1:
			break
		var x := PLAYHEAD_X + (bt - now) * px_per_sec
		var col := Color(0.24, 0.26, 0.34)
		var width := 1.0
		if absf(fposmod(b, 4.0)) < 0.001:
			col = Color(0.85, 0.87, 1.0, 0.9)
			width = 3.0
		elif absf(fposmod(b, 1.0)) < 0.001:
			col = Color(0.55, 0.58, 0.72)
			width = 2.0
		draw_line(Vector2(x, BAND_TOP), Vector2(x, BAND_BOTTOM), col, width)
		b += step

	# Events, dimmed when filtered out by the current grid resolution.
	for e in chart.events:
		var eb: float = float(e["beat"])
		var q := eb / step
		var in_grid := absf(q - roundf(q)) < 0.001
		var et := SongClock.grid_beat_to_time(eb) + float(e["nudge_ms"]) / 1000.0
		var x := PLAYHEAD_X + (et - now) * px_per_sec
		if x < -40.0 or x > size.x + 40.0:
			continue
		var y := lane_y - float(e["lane"]) * 60.0
		draw_rect(Rect2(x - 16.0, y - 16.0, 32.0, 32.0),
			Color(0.85, 0.3, 0.32, 1.0 if in_grid else 0.3))

	draw_line(Vector2(PLAYHEAD_X, BAND_TOP - 24.0),
		Vector2(PLAYHEAD_X, BAND_BOTTOM + 24.0), Color(0.95, 0.8, 0.3), 3.0)
