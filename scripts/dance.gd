extends Node2D
## Street-dance mode, cue-based. The chart prescribes tap moments detected
## from the track's actual snare/clap hits. Each cue is an approach circle
## that contracts onto the ring around the dancer; tap when it lands.
## Hitting cues builds the combo and escalates the choreography; an
## off-beat tap OR a missed cue trips him and resets the combo.
##
## All judgment is temporal, in smoothed song time, with two-tier windows
## from the JudgmentConfig resource, clamped so adjacent cue windows never
## overlap.

const RES_CHART_PATH := "res://charts/demo.chart.json"

const TIER_COMBO: Array[int] = [0, 3, 6, 10, 16]
const TIER_NAMES: Array[String] = ["", "TWITCHIN'!", "HARLEM SHAKE!!",
	"ZOMBIE MODE!!", "BREAKDANCE!!!"]
const PERFECT_WORDS: Array[String] = ["CLEAN!", "NICE!", "SHEESH!", "GROOVY!", "FUNKY!"]
const GOOD_WORDS: Array[String] = ["okay", "loose...", "close"]
const STUMBLE_WORDS: Array[String] = ["WHOA!", "OOF!", "MY SHOE!", "SPLAT!"]

const MAX_CROWD := 14
const APPROACH_BEATS := 2.0
const RING_R := 74.0

var cfg: JudgmentConfig = preload("res://config/game_config.tres")

var chart: Chart
var dancer: Dancer
var bg: ColorRect
var hud_label: Label
var combo_label: Label
var results_label: Label

var ground_y := 600.0
var center_x := 640.0

var cue_times: Array[float] = []   # song-time seconds, sorted
var cue_state: Array[int] = []     # 0 pending, 1 hit, 2 missed
var _next_pending := 0

var spectators: Array[Spectator] = []
var _crowd_serial := 0
var _next_crowd_beat := 0.0

var combo := 0
var best_combo := 0
var tier := 0
var score := 0
var money := 0
var hits := 0
var finished := false

var _popups: Array[Dictionary] = []
var _ring_flash_beat := -999.0
var _ring_flash_color := Color.WHITE


func _ready() -> void:
	var vs := get_viewport_rect().size
	ground_y = vs.y - 96.0
	center_x = vs.x / 2.0

	chart = Chart.load_from_file(RES_CHART_PATH)
	if chart == null:
		return
	SongClock.configure(chart.bpm, chart.offset_ms, chart.swing_percent)
	_load_cues()
	_build_scene()
	if not Stems.load_track(chart.stems, chart.bpm, int(ceilf(chart.end_beat())) + 8):
		return
	Stems.track_finished.connect(_on_track_finished)
	Stems.play_all()
	SongClock.start(Stems.instrumental)


func _exit_tree() -> void:
	Stems.stop_all()
	SongClock.stop()
	DebugOverlay.chain = 0


func _load_cues() -> void:
	var beats: Array[float] = []
	for e in chart.events:
		beats.append(float(e["beat"]))
	beats.sort()
	for b in beats:
		cue_times.append(SongClock.grid_beat_to_time(b))
		cue_state.append(0)


func _build_scene() -> void:
	var bg_layer := CanvasLayer.new()
	bg_layer.layer = -10
	bg = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bg.color = Color(0.07, 0.08, 0.12)
	bg_layer.add_child(bg)
	add_child(bg_layer)

	dancer = Dancer.new()
	dancer.position = Vector2(center_x, ground_y)
	add_child(dancer)

	var hud := CanvasLayer.new()
	hud.layer = 10
	add_child(hud)

	hud_label = Label.new()
	hud_label.anchor_left = 0.5
	hud_label.anchor_right = 0.5
	hud_label.offset_left = -400.0
	hud_label.offset_right = 400.0
	hud_label.offset_top = 12.0
	hud_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hud_label.add_theme_font_size_override("font_size", 30)
	hud.add_child(hud_label)

	combo_label = Label.new()
	combo_label.anchor_left = 0.5
	combo_label.anchor_right = 0.5
	combo_label.offset_left = -400.0
	combo_label.offset_right = 400.0
	combo_label.offset_top = 50.0
	combo_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	combo_label.add_theme_font_size_override("font_size", 24)
	combo_label.add_theme_color_override("font_color", Color(0.95, 0.8, 0.35))
	hud.add_child(combo_label)

	results_label = Label.new()
	results_label.set_anchors_preset(Control.PRESET_CENTER)
	results_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	results_label.add_theme_font_size_override("font_size", 38)
	results_label.visible = false
	hud.add_child(results_label)

	var back := Button.new()
	back.text = "MENU"
	back.anchor_top = 1.0
	back.anchor_bottom = 1.0
	back.offset_left = 12.0
	back.offset_right = 116.0
	back.offset_top = -56.0
	back.offset_bottom = -12.0
	back.pressed.connect(func() -> void:
		get_tree().change_scene_to_file("res://scenes/main_menu.tscn"))
	hud.add_child(back)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("bounce"):
		_on_tap()


func _on_tap() -> void:
	if finished:
		get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
		return
	if not SongClock.running:
		return
	Input.vibrate_handheld(15)
	if dancer.is_stumbling():
		return  # busy finding his shoe
	_judge_tap(SongClock.time() - Settings.calibration_offset_ms / 1000.0)


func _windows(gap_ms: float) -> Dictionary:
	var outer := minf(cfg.outer_ms(Settings.difficulty), gap_ms * 0.475)
	var inner := minf(cfg.inner_ms(Settings.difficulty), outer)
	return {"outer": outer, "inner": inner}


func _cue_window(i: int) -> Dictionary:
	var gap := INF
	if i > 0:
		gap = minf(gap, cue_times[i] - cue_times[i - 1])
	if i < cue_times.size() - 1:
		gap = minf(gap, cue_times[i + 1] - cue_times[i])
	return _windows((gap if gap < INF else 10.0) * 1000.0)


func _judge_tap(tap_time: float) -> void:
	# nearest pending cue around the tap
	var best := -1
	var best_dt := INF
	for i in range(maxi(0, _next_pending - 3), cue_times.size()):
		if cue_times[i] - tap_time > 1.2:
			break
		if cue_state[i] != 0:
			continue
		var dt := absf(tap_time - cue_times[i])
		if dt < best_dt:
			best_dt = dt
			best = i
	if best == -1:
		DebugOverlay.push_judgment(999.0)
		_stumble_tap(0.0, false)
		return
	var delta_ms := (tap_time - cue_times[best]) * 1000.0
	DebugOverlay.push_judgment(delta_ms)
	var w := _cue_window(best)
	if absf(delta_ms) <= float(w["inner"]):
		cue_state[best] = 1
		_hit(true, delta_ms)
	elif absf(delta_ms) <= float(w["outer"]):
		cue_state[best] = 1
		_hit(false, delta_ms)
	else:
		_stumble_tap(delta_ms, true)


func _hit(perfect: bool, delta_ms: float) -> void:
	var beat := SongClock.current_beat()
	combo += 1
	hits += 1
	best_combo = maxi(best_combo, combo)
	score += (100 if perfect else 40) * (tier + 1)
	Stems.open_drums_until(beat + cfg.drums_unmute_beats)
	Input.vibrate_handheld(45 if perfect else 25)
	_ring_flash_beat = beat
	_ring_flash_color = Color(0.5, 1.0, 0.6) if perfect else Color(0.95, 0.85, 0.4)
	var n := int(beat * 7.0) % PERFECT_WORDS.size()
	if perfect:
		_popup(PERFECT_WORDS[n], Color(0.5, 1.0, 0.6), 30)
	else:
		_popup("%s (%s %d ms)" % [GOOD_WORDS[n % GOOD_WORDS.size()],
			"late" if delta_ms > 0.0 else "early", int(absf(delta_ms))],
			Color(0.95, 0.85, 0.4), 22)
	_update_tier(beat)
	if tier >= 3 and combo % 2 == 0:
		_throw_money(tier - 1)


## Tapped with no cue in reach, or way off a cue's window.
func _stumble_tap(delta_ms: float, near_cue: bool) -> void:
	var beat := SongClock.current_beat()
	var n := int(beat * 5.0) % STUMBLE_WORDS.size()
	if near_cue:
		_popup("%s (%s %d ms)" % [STUMBLE_WORDS[n],
			"late" if delta_ms > 0.0 else "early", int(absf(delta_ms))],
			Color(1.0, 0.4, 0.35), 34)
	else:
		_popup(STUMBLE_WORDS[n] + " (no cue)", Color(1.0, 0.4, 0.35), 34)
	_stumble(beat, 1.2)


## A cue's window closed with no tap.
func _missed_cue() -> void:
	var beat := SongClock.current_beat()
	_popup("MISSED IT!", Color(1.0, 0.4, 0.35), 32)
	_stumble(beat, 1.0)


func _stumble(beat: float, duration_beats: float) -> void:
	combo = 0
	dancer.stumble_until = beat + duration_beats
	Input.vibrate_handheld(90)
	_ring_flash_beat = beat
	_ring_flash_color = Color(1.0, 0.35, 0.3)
	_update_tier(beat)
	var keep := spectators.size() / 2
	while spectators.size() > keep:
		var s: Spectator = spectators.pop_back()
		s.leaving = true


## Mark cues whose window has fully passed; the first one costs a stumble.
func _consume_overdue(now: float) -> void:
	while _next_pending < cue_times.size():
		if cue_state[_next_pending] != 0:
			_next_pending += 1
			continue
		var w := _cue_window(_next_pending)
		if now - cue_times[_next_pending] > float(w["outer"]) / 1000.0:
			cue_state[_next_pending] = 2
			_next_pending += 1
			if not dancer.is_stumbling():
				_missed_cue()
		else:
			break


func _update_tier(beat: float) -> void:
	var new_tier := 0
	for i in TIER_COMBO.size():
		if combo >= TIER_COMBO[i]:
			new_tier = i
	if new_tier > tier:
		_popup(TIER_NAMES[new_tier], Color(0.7, 0.85, 1.0), 44)
	tier = new_tier
	var level := 0
	for t in cfg.chain_thresholds:
		if combo >= t:
			level += 1
	Stems.set_escalation_level(level)


func _crowd_target() -> int:
	return mini(MAX_CROWD, combo + tier * 2)


func _manage_crowd(beat: float) -> void:
	if beat < _next_crowd_beat:
		return
	_next_crowd_beat = beat + 1.0
	if spectators.size() < _crowd_target():
		var s := Spectator.new()
		_crowd_serial += 1
		s.seed_n = _crowd_serial * 13
		s.phase = Spectator._hash01(_crowd_serial) * 0.6
		var side := -1.0 if _crowd_serial % 2 == 0 else 1.0
		var rank := spectators.size() / 2
		s.slot_pos = Vector2(
			center_x + side * (150.0 + 55.0 * rank + Spectator._hash01(_crowd_serial + 3) * 30.0),
			ground_y + 10.0)
		s.position = Vector2(center_x + side * 1400.0, s.slot_pos.y)
		add_child(s)
		spectators.append(s)


func _popup(text: String, color: Color, size: int) -> void:
	var beat := SongClock.current_beat()
	var n := _popups.size() + int(beat * 13.0)
	_popups.append({
		"text": text, "color": color, "size": size, "born": beat,
		"x": center_x + (Spectator._hash01(n) - 0.5) * 260.0,
		"y": ground_y - 220.0 - Spectator._hash01(n + 4) * 60.0,
	})


func _throw_money(count: int) -> void:
	if spectators.is_empty():
		return
	for i in count:
		var s: Spectator = spectators[(i * 7 + int(SongClock.current_beat())) % spectators.size()]
		s.cheer = 2
		var bill := MoneyBill.new()
		var land_x := center_x + (Spectator._hash01(i + int(SongClock.current_beat())) - 0.5) * 160.0
		bill.toss(s.position + Vector2(0.0, -60.0),
			Vector2(land_x, ground_y - 4.0), i % 3 == 0)
		add_child(bill)
		money += 25
		score += 25
	_popup("+$%d" % (count * 25), Color(0.55, 0.9, 0.55), 26)


func _process(_delta: float) -> void:
	if chart == null:
		return
	var beat := SongClock.current_beat()
	dancer.tier = tier
	dancer.set_beat(beat)
	var cheer_lv := clampi(tier - 1, 0, 2)
	for s in spectators:
		if not (tier >= 3 and s.cheer == 2):
			s.cheer = cheer_lv
		s.set_beat(beat)
	if SongClock.running:
		_consume_overdue(SongClock.time() - Settings.calibration_offset_ms / 1000.0)
		_manage_crowd(beat)
	for c in get_children():
		if c is Spectator and (c as Spectator).offscreen():
			c.queue_free()
	var base := Color(0.07, 0.08, 0.12).lerp(Color(0.25, 0.11, 0.27),
		clampf(tier / 4.0, 0.0, 1.0))
	var pulse := 0.0
	if tier > 0:
		pulse = 0.1 * tier * exp(-fposmod(beat, 4.0) * 5.0)
	bg.color = base.lightened(clampf(pulse, 0.0, 0.3))

	DebugOverlay.chain = combo
	hud_label.text = "SCORE %d      $%d      %s" % [score, money, Settings.difficulty.to_upper()]
	combo_label.text = "combo %d%s" % [combo,
		"  ·  " + TIER_NAMES[tier].to_lower() if tier > 0 else ""]
	while _popups.size() > 0 and beat - float(_popups[0]["born"]) > 2.0:
		_popups.pop_front()
	queue_redraw()


func _draw() -> void:
	if chart == null:
		return
	var vs := get_viewport_rect().size
	var beat := SongClock.current_beat()

	# alley backdrop
	var bx := -40.0
	var i := 0
	while bx < vs.x + 40.0:
		var h := ground_y * (0.35 + 0.4 * Spectator._hash01(i))
		var w := 150.0 + 130.0 * Spectator._hash01(i + 57)
		draw_rect(Rect2(bx, ground_y - h, w - 14.0, h), Color(0.10, 0.11, 0.16))
		for wi in 3:
			if Spectator._hash01(i * 7 + wi) > 0.45:
				draw_rect(Rect2(bx + 18.0 + wi * 40.0, ground_y - h + 22.0, 13.0, 17.0),
					Color(0.85, 0.75, 0.4, 0.25))
		bx += w
		i += 1
	draw_rect(Rect2(-40.0, ground_y, vs.x + 80.0, vs.y - ground_y + 40.0),
		Color(0.16, 0.17, 0.22))
	draw_line(Vector2(-40.0, ground_y + 1.5), Vector2(vs.x + 40.0, ground_y + 1.5),
		Color(0.32, 0.34, 0.42), 3.0)

	if not SongClock.running:
		return
	var focus := dancer.position + Vector2(0.0, -100.0)
	var now := SongClock.time() - Settings.calibration_offset_ms / 1000.0
	var horizon := APPROACH_BEATS * SongClock.beat_duration()

	# target ring (+ hit/stumble flash)
	var flash := clampf(1.0 - (beat - _ring_flash_beat) * 2.0, 0.0, 1.0)
	var ring_col := Color(0.85, 0.87, 1.0, 0.45).lerp(_ring_flash_color, flash)
	draw_arc(focus, RING_R, 0.0, TAU, 48, ring_col, 4.0 + 4.0 * flash)
	if flash > 0.0:
		draw_arc(focus, RING_R + (1.0 - flash) * 50.0, 0.0, TAU, 48,
			Color(_ring_flash_color, flash * 0.6), 3.0)

	# approach circles: each cue contracts onto the ring at its moment
	for ci in range(maxi(0, _next_pending - 4), cue_times.size()):
		var dt := cue_times[ci] - now
		if dt > horizon:
			break
		if cue_state[ci] != 0 or dt < -0.3:
			continue
		var f := clampf(dt / horizon, 0.0, 1.0)
		var r := RING_R + 250.0 * f
		var col := Color(0.95, 0.85, 0.4, clampf(1.1 - f, 0.25, 0.95))
		if dt < 0.0:
			col = Color(1.0, 0.5, 0.4, 0.9)  # window closing!
		draw_arc(focus, r, 0.0, TAU, 48, col, 5.0 if f < 0.35 else 3.0)

	# floating judgment popups
	var font := ThemeDB.fallback_font
	for pop in _popups:
		var age := beat - float(pop["born"])
		var alpha := clampf(1.2 - age * 0.6, 0.0, 1.0)
		var col: Color = pop["color"]
		var pos := Vector2(float(pop["x"]), float(pop["y"]) - age * 26.0)
		draw_string(font, pos, str(pop["text"]), HORIZONTAL_ALIGNMENT_CENTER,
			-1, int(pop["size"]), Color(col.r, col.g, col.b, alpha))


func _on_track_finished() -> void:
	finished = true
	SongClock.stop()
	results_label.text = "SHOW'S OVER\nscore %d — hits %d/%d — best combo %d\ncrowd %d — earned $%d\n\ntap to return" \
		% [score, hits, cue_times.size(), best_combo, spectators.size(), money]
	results_label.visible = true
