extends Node2D
## Street-dance mode. The dancer faces the camera; the player taps on the
## beat to keep him dancing. Combos escalate the choreography (bounce ->
## twitch -> Harlem shake -> zombie -> breakdance), draw a crowd, and get
## money thrown. An off-beat tap trips him and resets the combo.
##
## All judgment is temporal, against SongClock's smoothed song time at the
## current difficulty's grid resolution — same two-tier windows as ever,
## from the JudgmentConfig resource.

const RES_CHART_PATH := "res://charts/demo.chart.json"

## Combo needed for each dance tier (index = tier).
const TIER_COMBO: Array[int] = [0, 4, 8, 16, 32]
const TIER_NAMES: Array[String] = ["", "TWITCHIN'!", "HARLEM SHAKE!!",
	"ZOMBIE MODE!!", "BREAKDANCE!!!"]
const PERFECT_WORDS: Array[String] = ["CLEAN!", "NICE!", "SHEESH!", "GROOVY!", "FUNKY!"]
const GOOD_WORDS: Array[String] = ["okay", "loose...", "close"]
const STUMBLE_WORDS: Array[String] = ["WHOA!", "OOF!", "MY SHOE!", "SPLAT!"]

const MAX_CROWD := 14

var cfg: JudgmentConfig = preload("res://config/game_config.tres")

var chart: Chart
var dancer: Dancer
var bg: ColorRect
var hud_label: Label
var combo_label: Label
var results_label: Label

var ground_y := 600.0
var center_x := 640.0

var spectators: Array[Spectator] = []
var _crowd_serial := 0
var _next_crowd_beat := 0.0

var combo := 0
var best_combo := 0
var tier := 0
var score := 0
var money := 0
var finished := false

var _popups: Array[Dictionary] = []


func _ready() -> void:
	var vs := get_viewport_rect().size
	ground_y = vs.y - 96.0
	center_x = vs.x / 2.0

	chart = Chart.load_from_file(RES_CHART_PATH)
	if chart == null:
		return
	SongClock.configure(chart.bpm, chart.offset_ms, chart.swing_percent)
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
		return  # he's busy finding his shoe
	var tap_time := SongClock.time() - Settings.calibration_offset_ms / 1000.0
	var g := SongClock.nearest_grid(tap_time, Settings.grid_step())
	var delta_ms := float(g["delta_ms"])
	DebugOverlay.push_judgment(delta_ms)
	var w := _windows(float(g["gap_ms"]))
	var err := absf(delta_ms)
	if err <= float(w["inner"]):
		_hit(true, delta_ms)
	elif err <= float(w["outer"]):
		_hit(false, delta_ms)
	else:
		_stumble(delta_ms)


func _windows(gap_ms: float) -> Dictionary:
	var outer := minf(cfg.outer_ms(Settings.difficulty), gap_ms * 0.475)
	var inner := minf(cfg.inner_ms(Settings.difficulty), outer)
	return {"outer": outer, "inner": inner}


func _hit(perfect: bool, delta_ms: float) -> void:
	var beat := SongClock.current_beat()
	combo += 1
	best_combo = maxi(best_combo, combo)
	score += (100 if perfect else 40) * (tier + 1)
	Stems.open_drums_until(beat + cfg.drums_unmute_beats)
	Input.vibrate_handheld(45 if perfect else 25)
	var n := int(beat * 7.0) % PERFECT_WORDS.size()
	if perfect:
		_popup(PERFECT_WORDS[n], Color(0.5, 1.0, 0.6), 30)
	else:
		_popup("%s (%s %d ms)" % [GOOD_WORDS[n % GOOD_WORDS.size()],
			"late" if delta_ms > 0.0 else "early", int(absf(delta_ms))],
			Color(0.95, 0.85, 0.4), 22)
	_update_tier(beat)
	# high tiers get paid
	if tier >= 3 and combo % 4 == 0:
		_throw_money(tier - 1)


func _stumble(delta_ms: float) -> void:
	var beat := SongClock.current_beat()
	combo = 0
	dancer.stumble_until = beat + 1.5
	Input.vibrate_handheld(90)
	var n := int(beat * 5.0) % STUMBLE_WORDS.size()
	_popup("%s (%s %d ms)" % [STUMBLE_WORDS[n],
		"late" if delta_ms > 0.0 else "early", int(absf(delta_ms))],
		Color(1.0, 0.4, 0.35), 34)
	_update_tier(beat)
	# half the crowd wanders off, unimpressed
	var keep := spectators.size() / 2
	while spectators.size() > keep:
		var s: Spectator = spectators.pop_back()
		s.leaving = true


func _update_tier(beat: float) -> void:
	var new_tier := 0
	for i in TIER_COMBO.size():
		if combo >= TIER_COMBO[i]:
			new_tier = i
	if new_tier > tier:
		_popup(TIER_NAMES[new_tier], Color(0.7, 0.85, 1.0), 44)
	tier = new_tier
	# escalation stems track the combo exactly like the old chain system
	var level := 0
	for t in cfg.chain_thresholds:
		if combo >= t:
			level += 1
	Stems.set_escalation_level(level)


func _crowd_target() -> int:
	return mini(MAX_CROWD, combo / 2 + tier * 2)


func _manage_crowd(beat: float) -> void:
	if beat < _next_crowd_beat:
		return
	_next_crowd_beat = beat + 1.0
	var target := _crowd_target()
	if spectators.size() < target:
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
		"y": ground_y - 210.0 - Spectator._hash01(n + 4) * 60.0,
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
		if not (tier >= 3 and s.cheer == 2):  # money-throwers stay hyped
			s.cheer = cheer_lv
		s.set_beat(beat)
	if SongClock.running:
		_manage_crowd(beat)
	# retire spectators that finished walking away
	for c in get_children():
		if c is Spectator and (c as Spectator).offscreen():
			c.queue_free()
	# background: warmth and a downbeat pulse rise with the tier
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
	# prune old popups
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
	# sidewalk
	draw_rect(Rect2(-40.0, ground_y, vs.x + 80.0, vs.y - ground_y + 40.0),
		Color(0.16, 0.17, 0.22))
	draw_line(Vector2(-40.0, ground_y + 1.5), Vector2(vs.x + 40.0, ground_y + 1.5),
		Color(0.32, 0.34, 0.42), 3.0)

	if SongClock.running:
		# beat ring: contracts onto the target circle exactly on each beat
		var p := fposmod(beat, 1.0)
		var focus := dancer.position + Vector2(0.0, -100.0)
		var r := lerpf(230.0, 74.0, p)
		draw_arc(focus, r, 0.0, TAU, 48, Color(0.9, 0.9, 1.0, 0.20 + 0.15 * p), 3.0)
		var flash := maxf(0.0, 1.0 - p * 5.0)
		draw_arc(focus, 74.0, 0.0, TAU, 48,
			Color(0.95, 0.85, 0.4, 0.35 + 0.6 * flash), 4.0 + 5.0 * flash)

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
	results_label.text = "SHOW'S OVER\nscore %d — best combo %d\ncrowd %d — earned $%d\n\ntap to return" \
		% [score, best_combo, spectators.size(), money]
	results_label.visible = true
