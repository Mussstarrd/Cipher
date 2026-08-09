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
## Ticker: cues scroll right-to-left along the LED sidewalk strip and
## must be tapped when they reach the marker under the dancer's feet.
const TICKER_PX_PER_BEAT := 240.0
const STRIP_H := 46.0

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

## Trace (slide) challenges: a neon fuse on the left half. The shape pops
## up, sizzles for one beat, then ignites and burns start-to-end over
## duration_beats — keep a finger pressed on the flame as it travels. No
## grab step: any finger near the flame counts. Coverage accumulates in
## path-progress space (a pure function of the song clock), never frame
## delta.
const TRACE_RADIUS := 95.0     # finger-to-flame tolerance while burning
const TRACE_PASS := 0.6        # coverage fraction that counts as traced
const TRACE_LEAD_BEATS := 1.0  # shape pops up this long before ignition

var traces: Array[Dictionary] = []  # {start_t, dur_s, shape, pts, cum, covered, last_s, announced}
var trace_idx := 0
var _pointers: Dictionary = {}      # pointer id -> last canvas position

## Screen is split: left of this fraction is the trace zone (slides only,
## never tap-judged), right of it is the tap zone.
const TAP_ZONE_FRACTION := 0.40

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
	# Cue time = grid slot + per-cue nudge measured from the track's actual
	# hit transient (the chart generator aligns cues to the audio).
	var times: Array[float] = []
	for e in chart.events:
		if str(e["type"]) == "trace":
			var tr := {
				"start_t": SongClock.grid_beat_to_time(float(e["beat"])),
				"dur_s": float(e["duration_beats"]) * SongClock.beat_duration(),
				"shape": int(e["lane"]),
				"covered": 0.0,
				"last_s": 0.0,
				"announced": false,
			}
			_bake_trace_path(tr)
			traces.append(tr)
			continue
		times.append(SongClock.grid_beat_to_time(float(e["beat"]))
			+ float(e["nudge_ms"]) / 1000.0)
	times.sort()
	traces.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return float(a["start_t"]) < float(b["start_t"]))
	for t in times:
		cue_times.append(t)
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


## Multitouch routing: the trace needs one finger sliding while the other
## taps, so dance mode consumes raw touches (index-aware) instead of the
## single emulated mouse. Emulated mouse events are ignored to avoid
## double-firing; a real mouse (desktop) maps to pointer id 100; keyboard
## "bounce" stays a plain tap.
func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		if event.pressed:
			_pointer_down(event.index, event.position)
		else:
			_pointer_up(event.index)
	elif event is InputEventScreenDrag:
		_pointer_move(event.index, event.position)
	elif event is InputEventMouseButton and event.device != InputEvent.DEVICE_ID_EMULATION:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				_pointer_down(100, event.position)
			else:
				_pointer_up(100)
	elif event is InputEventMouseMotion and event.device != InputEvent.DEVICE_ID_EMULATION:
		if _pointers.has(100):
			_pointer_move(100, event.position)
	elif event is InputEventKey and event.is_action_pressed("bounce"):
		_on_tap()


func _pointer_down(id: int, pos: Vector2) -> void:
	_pointers[id] = pos
	if finished:
		_on_tap()
		return
	# left of the divider is trace territory: never judged as a tap, so a
	# finger riding (or waiting for) a fuse can't trip the dancer
	if pos.x < get_viewport_rect().size.x * TAP_ZONE_FRACTION:
		return
	_on_tap()


func _pointer_move(id: int, pos: Vector2) -> void:
	_pointers[id] = pos


func _pointer_up(id: int) -> void:
	_pointers.erase(id)


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


## Path progress for the current visual timeline; <0 during the sizzle
## lead-in, 0 at ignition, 1 when the fuse is fully burned.
func _trace_progress(tr: Dictionary) -> float:
	var now := SongClock.time() - Settings.calibration_offset_ms / 1000.0 \
		- Settings.av_offset_ms / 1000.0
	return (now - float(tr["start_t"])) / float(tr["dur_s"])


## Fuse shapes, as waypoint polylines in the trace zone.
## 0 vert down, 1 vert up, 2 horiz L->R, 3 horiz R->L, 4 zigzag,
## 5 Z-shape, 6 S-curve.
func _bake_trace_path(tr: Dictionary) -> void:
	var xl := 90.0
	var xr := 430.0
	var cx := (xl + xr) / 2.0
	var yt := ground_y * 0.30
	var yb := ground_y - 70.0
	var ym := (yt + yb) / 2.0
	var pts := PackedVector2Array()
	match int(tr["shape"]):
		0:
			pts = PackedVector2Array([Vector2(cx, yt), Vector2(cx, yb)])
		1:
			pts = PackedVector2Array([Vector2(cx, yb), Vector2(cx, yt)])
		2:
			pts = PackedVector2Array([Vector2(xl, ym), Vector2(xr, ym)])
		3:
			pts = PackedVector2Array([Vector2(xr, ym), Vector2(xl, ym)])
		4:  # zigzag down
			pts = PackedVector2Array([
				Vector2(xl + 40.0, yt), Vector2(xr - 40.0, yt + (yb - yt) * 0.33),
				Vector2(xl + 40.0, yt + (yb - yt) * 0.66), Vector2(xr - 40.0, yb)])
		5:  # a proper Z
			pts = PackedVector2Array([
				Vector2(xl + 30.0, yt), Vector2(xr - 30.0, yt),
				Vector2(xl + 30.0, yb), Vector2(xr - 30.0, yb)])
		_:  # S-curve, sampled into a polyline
			for i in 25:
				var s := float(i) / 24.0
				pts.append(Vector2(cx + 85.0 * sin(s * TAU * 0.75),
					lerpf(yt, yb, s)))
	var cum := PackedFloat32Array([0.0])
	for i in range(1, pts.size()):
		cum.append(cum[i - 1] + pts[i - 1].distance_to(pts[i]))
	tr["pts"] = pts
	tr["cum"] = cum


func _trace_path(tr: Dictionary, s: float) -> Vector2:
	var pts: PackedVector2Array = tr["pts"]
	var cum: PackedFloat32Array = tr["cum"]
	var target := clampf(s, 0.0, 1.0) * cum[cum.size() - 1]
	for i in range(1, pts.size()):
		if cum[i] >= target:
			var seg := cum[i] - cum[i - 1]
			var f := 0.0 if seg <= 0.0 else (target - cum[i - 1]) / seg
			return pts[i - 1].lerp(pts[i], f)
	return pts[pts.size() - 1]


func _update_trace(beat: float) -> void:
	if trace_idx >= traces.size():
		return
	var tr := traces[trace_idx]
	var s_now := _trace_progress(tr)
	if s_now >= 1.0:
		_finish_trace(tr)
		trace_idx += 1
		return
	var lead := TRACE_LEAD_BEATS * SongClock.beat_duration() / float(tr["dur_s"])
	if s_now >= -lead and not bool(tr["announced"]):
		tr["announced"] = true
		_popup("FOLLOW THE FUSE!", Color(0.4, 1.0, 0.85), 30)
		Input.vibrate_handheld(20)
	if s_now < 0.0:
		return
	# coverage advances only while some finger rides the flame
	var s_clamped := clampf(s_now, 0.0, 1.0)
	if _finger_on_flame(tr, s_clamped):
		tr["covered"] = float(tr["covered"]) + maxf(0.0, s_clamped - float(tr["last_s"]))
		# gentle half-beat tick while riding
		if beat - float(tr.get("hap_beat", -99.0)) >= 0.5:
			tr["hap_beat"] = beat
			Input.vibrate_handheld(8)
	tr["last_s"] = s_clamped


func _finger_on_flame(tr: Dictionary, s: float) -> bool:
	var flame := _trace_path(tr, s)
	for pos in _pointers.values():
		if (pos as Vector2).distance_to(flame) < TRACE_RADIUS:
			return true
	return false


func _finish_trace(tr: Dictionary) -> void:
	var ratio := float(tr["covered"])
	if ratio >= TRACE_PASS:
		var beat := SongClock.current_beat()
		combo += 2
		hits += 1
		best_combo = maxi(best_combo, combo)
		var bonus := 250 * (tier + 1)
		score += bonus
		Stems.open_drums_until(beat + 1.0)
		Input.vibrate_handheld(60)
		_ring_flash_beat = beat
		_ring_flash_color = Color(0.4, 1.0, 0.85)
		_popup("TRACED! +%d" % bonus, Color(0.4, 1.0, 0.85), 34)
		_update_tier(beat)
	else:
		_popup("SLOPPY TRACE! (%d%%)" % int(ratio * 100.0), Color(1.0, 0.4, 0.35), 32)
		_stumble(SongClock.current_beat(), 1.2)


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
		var slot_x := center_x + side * (150.0 + 55.0 * rank
			+ Spectator._hash01(_crowd_serial + 3) * 30.0)
		# the trace zone stays clear — left-side crowd bunches up at its edge
		var zone_edge := get_viewport_rect().size.x * TAP_ZONE_FRACTION + 50.0
		if slot_x < zone_edge:
			slot_x = zone_edge + Spectator._hash01(_crowd_serial + 7) * 40.0
		s.slot_pos = Vector2(slot_x, ground_y + 10.0)
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
		_update_trace(beat)
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

	_draw_backdrop(vs, beat)
	_draw_shadows()
	_draw_ticker(vs, beat)
	_draw_zone_divider(vs)
	_draw_traces()
	_draw_popups(beat)


func _draw_backdrop(vs: Vector2, beat: float) -> void:
	# night-sky gradient: translucent bands let the tier-pulse ColorRect
	# glow through near the skyline
	var top := Color(0.03, 0.03, 0.09)
	var horizon_col := Color(0.12, 0.08, 0.16)
	var bands := 10
	for bi in bands:
		var f0 := float(bi) / bands
		var c := top.lerp(horizon_col, f0)
		c.a = 0.9 - f0 * 0.75
		draw_rect(Rect2(-40.0, -40.0 + (ground_y + 40.0) * f0,
			vs.x + 80.0, (ground_y + 40.0) / bands + 1.0), c)
	# moon + stars
	var moon := Vector2(vs.x * 0.82, ground_y * 0.18)
	draw_circle(moon, 34.0, Color(0.92, 0.9, 0.8, 0.12))
	draw_circle(moon, 26.0, Color(0.92, 0.9, 0.82))
	draw_circle(moon + Vector2(-9.0, -4.0), 5.0, Color(0.8, 0.78, 0.7))
	draw_circle(moon + Vector2(7.0, 8.0), 3.5, Color(0.8, 0.78, 0.7))
	for si in 26:
		var sp := Vector2(Spectator._hash01(si * 3) * vs.x,
			Spectator._hash01(si * 3 + 1) * ground_y * 0.5)
		var tw := 0.35 + 0.3 * sin(beat * 2.0 + si * 1.7)
		draw_circle(sp, 1.6, Color(0.9, 0.9, 1.0, tw))

	# far building layer
	var bx := -60.0
	var i := 100
	while bx < vs.x + 60.0:
		var h := ground_y * (0.5 + 0.35 * Spectator._hash01(i))
		var w := 190.0 + 150.0 * Spectator._hash01(i + 57)
		draw_rect(Rect2(bx, ground_y - h, w - 10.0, h), Color(0.07, 0.075, 0.115))
		bx += w
		i += 1
	# near building layer with lit windows and rooftop clutter
	bx = -40.0
	i = 0
	while bx < vs.x + 40.0:
		var h := ground_y * (0.3 + 0.34 * Spectator._hash01(i))
		var w := 150.0 + 130.0 * Spectator._hash01(i + 57)
		draw_rect(Rect2(bx, ground_y - h, w - 14.0, h), Color(0.10, 0.11, 0.16))
		draw_rect(Rect2(bx, ground_y - h, w - 14.0, 5.0), Color(0.14, 0.15, 0.21))
		for wy in 3:
			for wi in 3:
				if Spectator._hash01(i * 17 + wy * 5 + wi) > 0.55:
					draw_rect(Rect2(bx + 18.0 + wi * 40.0,
						ground_y - h + 22.0 + wy * 42.0, 13.0, 17.0),
						Color(0.85, 0.75, 0.4, 0.22 + 0.12 * Spectator._hash01(wi + wy)))
		if Spectator._hash01(i + 200) > 0.6:
			var ax := bx + (w - 14.0) * 0.5
			draw_line(Vector2(ax, ground_y - h), Vector2(ax, ground_y - h - 34.0),
				Color(0.16, 0.17, 0.23), 3.0)
		bx += w
		i += 1

	# sidewalk
	draw_rect(Rect2(-40.0, ground_y, vs.x + 80.0, vs.y - ground_y + 40.0),
		Color(0.15, 0.16, 0.21))
	draw_line(Vector2(-40.0, ground_y + 1.5), Vector2(vs.x + 40.0, ground_y + 1.5),
		Color(0.30, 0.32, 0.40), 3.0)

	# street lamps flanking the show, with warm light pools
	for side: float in [-1.0, 1.0]:
		var lx := center_x + side * 430.0
		draw_line(Vector2(lx, ground_y), Vector2(lx, ground_y - 250.0),
			Color(0.2, 0.21, 0.27), 6.0)
		draw_line(Vector2(lx, ground_y - 250.0),
			Vector2(lx - side * 42.0, ground_y - 262.0), Color(0.2, 0.21, 0.27), 5.0)
		var head := Vector2(lx - side * 48.0, ground_y - 258.0)
		draw_circle(head, 9.0, Color(1.0, 0.85, 0.5))
		draw_circle(head, 22.0, Color(1.0, 0.85, 0.5, 0.10))
		draw_circle(head, 44.0, Color(1.0, 0.85, 0.5, 0.05))
		draw_set_transform(Vector2(head.x, ground_y + 8.0), 0.0, Vector2(1.0, 0.22))
		draw_circle(Vector2.ZERO, 95.0, Color(1.0, 0.85, 0.5, 0.06))
		draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


func _draw_shadows() -> void:
	for who in ([dancer] + spectators):
		if who == null or not is_instance_valid(who):
			continue
		draw_set_transform(Vector2(who.position.x, who.position.y + 4.0),
			0.0, Vector2(1.0, 0.25))
		draw_circle(Vector2.ZERO, 34.0 if who == dancer else 24.0,
			Color(0.0, 0.0, 0.0, 0.30))
		draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


func _draw_ticker(vs: Vector2, beat: float) -> void:
	if not SongClock.running:
		return
	# LED strip on the pavement, right under his feet
	var y0 := ground_y + 22.0
	var strip := Rect2(-40.0, y0, vs.x + 80.0, STRIP_H)
	draw_rect(strip, Color(0.05, 0.06, 0.09))
	var neon := Color(0.25, 0.85, 0.95)
	for edge_y: float in [y0, y0 + STRIP_H]:
		draw_line(Vector2(-40.0, edge_y), Vector2(vs.x + 40.0, edge_y),
			Color(neon, 0.12), 8.0)
		draw_line(Vector2(-40.0, edge_y), Vector2(vs.x + 40.0, edge_y),
			Color(neon, 0.5), 2.5)
	# dim LED dot matrix
	var dx := 0.0
	while dx < vs.x:
		draw_circle(Vector2(dx, y0 + STRIP_H * 0.5), 1.5, Color(neon, 0.10))
		dx += 18.0

	# visual timeline: audio clock shifted by the player's A/V nudge
	var now := SongClock.time() - Settings.calibration_offset_ms / 1000.0 \
		- Settings.av_offset_ms / 1000.0
	var pps := TICKER_PX_PER_BEAT / SongClock.beat_duration()
	var cy := y0 + STRIP_H * 0.5

	# scrolling beat ticks so the pulse is readable between cues
	var t_left := now - (center_x + 60.0) / pps
	var b0 := floorf(SongClock.linear_beat_at(t_left))
	for k in 24:
		var bt := SongClock.grid_beat_to_time(b0 + k)
		var x := center_x + (bt - now) * pps
		if x < -40.0 or x > vs.x + 40.0:
			continue
		var downbeat := absf(fposmod(b0 + k, 4.0)) < 0.01
		draw_line(Vector2(x, y0 + 6.0), Vector2(x, y0 + STRIP_H - 6.0),
			Color(0.5, 0.55, 0.7, 0.35 if downbeat else 0.15),
			3.0 if downbeat else 1.5)

	# hit marker under his feet (+ judgment flash)
	var flash := clampf(1.0 - (beat - _ring_flash_beat) * 2.0, 0.0, 1.0)
	var mk := Color(0.95, 0.4, 0.9).lerp(_ring_flash_color, flash)
	draw_rect(Rect2(center_x - 26.0, y0 - 4.0, 52.0, STRIP_H + 8.0),
		Color(mk, 0.10 + 0.25 * flash))
	draw_line(Vector2(center_x, y0 - 8.0), Vector2(center_x, y0 + STRIP_H + 8.0),
		Color(mk, 0.25), 9.0)
	draw_line(Vector2(center_x, y0 - 8.0), Vector2(center_x, y0 + STRIP_H + 8.0),
		mk, 3.0)
	if flash > 0.0:
		draw_circle(Vector2(center_x, cy), 30.0 + (1.0 - flash) * 46.0,
			Color(_ring_flash_color, flash * 0.35))
		# the dancer lights up too
		draw_circle(dancer.position + Vector2(0.0, -70.0), 90.0,
			Color(_ring_flash_color, flash * 0.10))

	# cue pads scrolling toward the marker
	for ci in range(maxi(0, _next_pending - 6), cue_times.size()):
		var x := center_x + (cue_times[ci] - now) * pps
		if x > vs.x + 60.0:
			break
		if x < -60.0 or cue_state[ci] == 1:
			continue
		var missed := cue_state[ci] == 2
		var closing: bool = (cue_times[ci] - now) < 0.0
		var col := Color(1.0, 0.85, 0.3)
		if missed:
			col = Color(0.6, 0.25, 0.22)
		elif closing:
			col = Color(1.0, 0.5, 0.35)
		# neon pad with glow
		draw_circle(Vector2(x, cy), 20.0, Color(col, 0.16))
		draw_circle(Vector2(x, cy), 13.0, Color(col, 0.55))
		draw_circle(Vector2(x, cy), 8.0, col)
		draw_circle(Vector2(x, cy - 3.0), 3.0, Color(1.0, 1.0, 0.9, 0.8))


func _draw_popups(beat: float) -> void:
	var font := ThemeDB.fallback_font
	for pop in _popups:
		var age := beat - float(pop["born"])
		var alpha := clampf(1.2 - age * 0.6, 0.0, 1.0)
		var col: Color = pop["color"]
		var pos := Vector2(float(pop["x"]), float(pop["y"]) - age * 26.0)
		var text := str(pop["text"])
		draw_string(font, pos + Vector2(2.0, 2.0), text,
			HORIZONTAL_ALIGNMENT_CENTER, -1, int(pop["size"]),
			Color(0.0, 0.0, 0.0, alpha * 0.6))
		draw_string(font, pos, text, HORIZONTAL_ALIGNMENT_CENTER,
			-1, int(pop["size"]), Color(col.r, col.g, col.b, alpha))


func _draw_zone_divider(vs: Vector2) -> void:
	# faint dashed divider between the trace side and the tap side
	var dx := vs.x * TAP_ZONE_FRACTION
	var dy := 20.0
	while dy < ground_y - 10.0:
		draw_line(Vector2(dx, dy), Vector2(dx, dy + 14.0),
			Color(0.6, 0.7, 0.9, 0.10), 2.0)
		dy += 30.0
	# when a fuse is inbound or burning, tint its territory so the left
	# hand knows to get ready
	if SongClock.running and trace_idx < traces.size():
		var tr := traces[trace_idx]
		var lead := TRACE_LEAD_BEATS * SongClock.beat_duration() / float(tr["dur_s"])
		var s_now := _trace_progress(tr)
		if s_now > -lead and s_now < 1.0:
			var a := clampf((s_now + lead) / lead, 0.0, 1.0) * 0.05
			draw_rect(Rect2(-40.0, 0.0, dx + 40.0, ground_y),
				Color(0.35, 1.0, 0.8, a))


func _draw_traces() -> void:
	if not SongClock.running or trace_idx >= traces.size():
		return
	var tr := traces[trace_idx]
	var s_now := _trace_progress(tr)
	var lead := TRACE_LEAD_BEATS * SongClock.beat_duration() / float(tr["dur_s"])
	if s_now < -lead or s_now >= 1.0:
		return
	var beat := SongClock.current_beat()
	# the shape pops in fast, then the fuse ignites at s = 0
	var appear := clampf((s_now + lead) / lead * 2.0, 0.0, 1.0)
	var s_clamped := clampf(s_now, 0.0, 1.0)
	var neon := Color(0.35, 1.0, 0.8)
	var ember := Color(1.0, 0.45, 0.15)
	var riding := _finger_on_flame(tr, s_clamped)

	# unburned fuse ahead: neon; burned behind: dark ember trail
	var segs := 48
	var prev := _trace_path(tr, 0.0)
	for i in range(1, segs + 1):
		var s := float(i) / segs
		var p := _trace_path(tr, s)
		if s < s_clamped:  # already burned away
			draw_line(prev, p, Color(0.25, 0.12, 0.08, appear * 0.7), 5.0)
			if Spectator._hash01(i * 31) > 0.6:  # glowing embers
				var die := Spectator._hash01(i * 7 + int(beat * 2.0)) * 0.5
				draw_circle(p, 2.5, Color(ember, appear * (0.3 + die)))
		else:
			draw_line(prev, p, Color(neon, appear * 0.20), 15.0)
			draw_line(prev, p, Color(neon, appear * 0.9), 5.0)
		prev = p

	var flame := _trace_path(tr, s_clamped)
	if s_now < 0.0:
		# sizzling at the start point, about to ignite
		var sputter := 0.7 + 0.3 * sin(beat * 40.0)
		draw_circle(flame, 26.0 * sputter, Color(ember, appear * 0.25))
		draw_circle(flame, 12.0 * sputter, Color(1.0, 0.8, 0.3, appear * 0.9))
		draw_circle(flame, 5.0, Color(1.0, 1.0, 0.85, appear))
		return

	# the burning flame head: flickers, sparks, and judges you
	var flick := 0.8 + 0.35 * Spectator._hash01(int(beat * 16.0))
	var fcol := Color(1.0, 0.75, 0.25) if riding else Color(1.0, 0.45, 0.3)
	draw_circle(flame, 34.0 * flick, Color(fcol, 0.16))
	draw_circle(flame, 19.0 * flick, Color(fcol, 0.55))
	draw_circle(flame, 10.0, Color(1.0, 0.95, 0.75))
	draw_circle(flame, 4.5, Color(1.0, 1.0, 1.0))
	# sparks spraying off the flame
	for si in 5:
		var sh := Spectator._hash01(si * 13 + int(beat * 12.0))
		var ang := sh * TAU
		var dist := 16.0 + 34.0 * Spectator._hash01(si * 29 + int(beat * 12.0))
		draw_circle(flame + Vector2(cos(ang), sin(ang)) * dist,
			2.0, Color(1.0, 0.85, 0.4, 0.7 * (1.0 - dist / 50.0)))
	# finger-off warning ring
	if not riding:
		draw_arc(flame, TRACE_RADIUS, 0.0, TAU, 48, Color(1.0, 0.4, 0.3, 0.25), 2.0)


func _on_track_finished() -> void:
	finished = true
	SongClock.stop()
	results_label.text = "SHOW'S OVER\nscore %d — hits %d/%d — best combo %d\ncrowd %d — earned $%d\n\ntap to return" \
		% [score, hits, cue_times.size(), best_combo, spectators.size(), money]
	results_label.visible = true
