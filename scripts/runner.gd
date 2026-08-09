extends Node2D
## Generic runner scene: consumes any chart JSON. Everything gameplay-
## critical is a function of SongClock's smoothed song time — the player's
## x position, the jump arc, judgment, and miss deadlines. Frame delta is
## never consulted for any of it.

const GROUND_Y := 520.0
const CAMERA_LEAD := 320.0  # player's distance from the left screen edge
const LANE_HEIGHT := 90.0
const USER_CHART_PATH := "user://charts/demo.chart.json"
const RES_CHART_PATH := "res://charts/dragon.chart.json"

var cfg: JudgmentConfig = preload("res://config/game_config.tres")

var chart: Chart
var player: RunnerPlayer
var camera: Camera2D
var bg: ColorRect
var hud_label: Label
var toast_label: Label
var results_label: Label

var enemies: Array[RunnerEnemy] = []
var coins_flying: Array[RunnerCoin] = []

var chain := 0
var max_chain := 0
var multiplier := 1
var coin_count := 0
var invuln_until_beat := -999.0
var finished := false


func _ready() -> void:
	coin_count = cfg.start_coins
	chart = _load_chart()
	if chart == null:
		return
	SongClock.configure(chart.bpm, chart.offset_ms, chart.swing_percent)
	_build_world()
	_build_hud()
	if not Stems.load_track(chart.stems, chart.bpm, int(ceilf(chart.end_beat())) + 8):
		toast("Could not load audio stems — check chart paths")
		return
	Stems.track_finished.connect(_on_track_finished)
	Stems.play_all()
	SongClock.start(Stems.instrumental)
	queue_redraw()


func _exit_tree() -> void:
	Stems.stop_all()
	SongClock.stop()
	DebugOverlay.chain = 0


## The authoring tool writes to user://; prefer that copy so authored
## charts are immediately playable.
func _load_chart() -> Chart:
	if FileAccess.file_exists(USER_CHART_PATH):
		var c := Chart.load_from_file(USER_CHART_PATH)
		if c != null:
			return c
	return Chart.load_from_file(RES_CHART_PATH)


func _build_world() -> void:
	var bg_layer := CanvasLayer.new()
	bg_layer.layer = -10
	bg = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE  # taps must reach gameplay
	bg.color = Color(0.07, 0.08, 0.12)
	bg_layer.add_child(bg)
	add_child(bg_layer)

	player = RunnerPlayer.new()
	player.ground_y = GROUND_Y
	player.jump_height = cfg.jump_height_px
	player.arc_beats = cfg.jump_duration_beats
	add_child(player)

	for e in chart.events_for_step(Settings.grid_step()):
		var enemy := RunnerEnemy.new()
		enemy.beat = float(e["beat"])
		var t := SongClock.grid_beat_to_time(enemy.beat) \
			+ float(e["nudge_ms"]) / 1000.0
		enemy.position = Vector2(
			SongClock.linear_beat_at(t) * cfg.px_per_beat,
			GROUND_Y - float(e["lane"]) * LANE_HEIGHT)
		add_child(enemy)
		enemies.append(enemy)

	camera = Camera2D.new()
	camera.position = Vector2(640.0 - CAMERA_LEAD, 360.0)
	add_child(camera)
	camera.make_current()


func _build_hud() -> void:
	var hud := CanvasLayer.new()
	hud.layer = 10
	add_child(hud)

	hud_label = Label.new()
	hud_label.anchor_left = 0.5
	hud_label.anchor_right = 0.5
	hud_label.offset_left = -300.0
	hud_label.offset_right = 300.0
	hud_label.offset_top = 14.0
	hud_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hud_label.add_theme_font_size_override("font_size", 26)
	hud.add_child(hud_label)

	toast_label = Label.new()
	toast_label.set_anchors_preset(Control.PRESET_CENTER)
	toast_label.offset_top = 120.0
	toast_label.offset_bottom = 160.0
	toast_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	toast_label.add_theme_font_size_override("font_size", 24)
	toast_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.4))
	hud.add_child(toast_label)

	results_label = Label.new()
	results_label.set_anchors_preset(Control.PRESET_CENTER)
	results_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	results_label.add_theme_font_size_override("font_size", 40)
	results_label.visible = false
	hud.add_child(results_label)

	var back := Button.new()
	back.text = "MENU"
	back.anchor_top = 1.0
	back.anchor_bottom = 1.0
	back.offset_left = 12.0
	back.offset_right = 110.0
	back.offset_top = -54.0
	back.offset_bottom = -12.0
	back.pressed.connect(_back_to_menu)
	hud.add_child(back)


func _draw() -> void:
	if chart == null:
		return
	var end_x := (chart.end_beat() + 8.0) * cfg.px_per_beat

	# Alley skyline silhouettes (deterministic pseudo-random heights).
	var bx := -600.0
	var i := 0
	while bx < end_x + 800.0:
		var h := 150.0 + 190.0 * _hash01(i)
		var w := 170.0 + 150.0 * _hash01(i + 57)
		draw_rect(Rect2(bx, GROUND_Y - h, w - 16.0, h), Color(0.10, 0.11, 0.16))
		# a few lit windows
		for wi in 3:
			if _hash01(i * 7 + wi) > 0.45:
				draw_rect(Rect2(bx + 20.0 + wi * 42.0, GROUND_Y - h + 24.0, 14.0, 18.0),
					Color(0.85, 0.75, 0.4, 0.25))
		bx += w
		i += 1

	# Sidewalk with a curb line; seams mark the beats.
	draw_rect(Rect2(-2000.0, GROUND_Y, end_x + 4000.0, 400.0),
		Color(0.16, 0.17, 0.22))
	draw_line(Vector2(-2000.0, GROUND_Y + 1.5), Vector2(end_x + 2000.0, GROUND_Y + 1.5),
		Color(0.32, 0.34, 0.42), 3.0)
	for b in int(chart.end_beat()) + 9:
		var x := b * cfg.px_per_beat
		var downbeat := b % 4 == 0
		draw_line(Vector2(x, GROUND_Y), Vector2(x, GROUND_Y + 16.0),
			Color(0.45, 0.48, 0.6, 0.9 if downbeat else 0.35),
			3.0 if downbeat else 1.5)


func _hash01(n: int) -> float:
	return fposmod(sin(float(n) * 12.9898) * 43758.5453, 1.0)


func _process(_delta: float) -> void:
	if chart == null:
		return
	if SongClock.running:
		var beat := SongClock.current_beat()
		player.advance(beat, beat * cfg.px_per_beat)
		camera.position = Vector2(player.position.x + (640.0 - CAMERA_LEAD), 360.0)
		_check_collisions(beat)
		_collect_coins()
		_update_bg(beat)
	DebugOverlay.chain = chain
	hud_label.text = "%s    COINS %d    CHAIN %d    x%d" \
		% [Settings.difficulty.to_upper(), coin_count, chain, multiplier]


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("bounce"):
		_on_tap()


func _on_tap() -> void:
	if finished:
		_back_to_menu()
		return
	if not SongClock.running:
		return
	Input.vibrate_handheld(15)
	# Temporal gate input: tap timestamp in smoothed song time, shifted by
	# the player's stored calibration offset.
	var tap_time := SongClock.time() - Settings.calibration_offset_ms / 1000.0
	var g := SongClock.nearest_grid(tap_time, Settings.grid_step())
	DebugOverlay.push_judgment(float(g["delta_ms"]))

	var grace_beats := cfg.landing_grace_ms / 1000.0 / SongClock.beat_duration()
	var target := _spatial_candidate()
	if target != null and player.can_bounce(SongClock.current_beat(), grace_beats):
		_try_bounce(target, g)
	elif not player.air:
		player.begin_arc(SongClock.current_beat())


## Spatial gate: the player's feet must be inside an enemy's expanded
## head zone.
func _spatial_candidate() -> RunnerEnemy:
	var f := player.feet()
	for enemy in enemies:
		if not enemy.resolved and enemy.head_zone(cfg.spatial_threshold_px).has_point(f):
			return enemy
	return null


## Temporal gate: |tap - nearest grid slot| within the (density-clamped)
## windows. Inner window feeds the chain; outer only keeps you alive.
func _try_bounce(target: RunnerEnemy, g: Dictionary) -> void:
	var w := _windows(float(g["gap_ms"]))
	var err := absf(float(g["delta_ms"]))
	if err <= float(w["inner"]):
		_bounce(target, true)
	elif err <= float(w["outer"]):
		_bounce(target, false)


func _windows(gap_ms: float) -> Dictionary:
	var outer := minf(cfg.outer_ms(Settings.difficulty), gap_ms * 0.475)
	var inner := minf(cfg.inner_ms(Settings.difficulty), outer)
	return {"outer": outer, "inner": inner}


func _bounce(enemy: RunnerEnemy, tight: bool) -> void:
	enemy.squash()
	player.begin_arc(SongClock.current_beat())
	Input.vibrate_handheld(45)
	Stems.open_drums_until(SongClock.current_beat() + cfg.drums_unmute_beats)
	if tight:
		chain += 1
		max_chain = maxi(max_chain, chain)
		_apply_escalation()


func _check_collisions(beat: float) -> void:
	if finished or beat < invuln_until_beat:
		return
	var body := player.body_rect()
	for enemy in enemies:
		if enemy.resolved or not enemy.body_rect().intersects(body):
			continue
		# An airborne overlap can still be saved by a bounce until the
		# enemy's outer judgment window has fully passed.
		var g := SongClock.nearest_grid(
			SongClock.grid_beat_to_time(enemy.beat), Settings.grid_step())
		var deadline: float = float(g["time"]) \
			+ float(_windows(float(g["gap_ms"]))["outer"]) / 1000.0
		if not player.air or SongClock.time() > deadline:
			_miss(enemy, beat)
			return


func _miss(enemy: RunnerEnemy, beat: float) -> void:
	enemy.consume_miss()
	Input.vibrate_handheld(90)
	chain = 0
	_apply_escalation()  # chain break strips escalation stems back
	invuln_until_beat = beat + 1.0
	player.knock_until_beat = beat + 0.5
	if coin_count <= 0:
		toast("OUT OF COINS — death stubbed for POC, refilling")
		coin_count = cfg.start_coins
		return
	var dropped := mini(cfg.coins_dropped_on_miss, coin_count)
	coin_count -= dropped
	for i in dropped:
		var c := RunnerCoin.new()
		var frac := float(i) / maxf(1.0, float(dropped - 1))
		var ang := -PI * 0.25 - PI * 0.5 * frac  # fan upward
		c.launch(player.position + Vector2(0.0, -40.0),
			Vector2(cos(ang), sin(ang)) * (260.0 + 40.0 * float(i % 3)),
			cfg.coin_recollect_seconds)
		add_child(c)
		coins_flying.append(c)


func _collect_coins() -> void:
	for c in coins_flying.duplicate():
		if not is_instance_valid(c):
			coins_flying.erase(c)
		elif c.collectable() and c.position.distance_to(player.position) < 70.0:
			coin_count += 1
			coins_flying.erase(c)
			c.queue_free()


func _apply_escalation() -> void:
	var level := 0
	for t in cfg.chain_thresholds:
		if chain >= t:
			level += 1
	multiplier = level + 1
	Stems.set_escalation_level(level)


## Visual intensity for POC: background color shift with chain level and a
## screen pulse on downbeats — both pure functions of the song beat.
func _update_bg(beat: float) -> void:
	var level := multiplier - 1
	var base := Color(0.07, 0.08, 0.12).lerp(
		Color(0.24, 0.10, 0.26), clampf(level / 3.0, 0.0, 1.0))
	var pulse := 0.0
	if level > 0:
		pulse = 0.12 * float(level) * exp(-fposmod(beat, 4.0) * 5.0)
	bg.color = base.lightened(clampf(pulse, 0.0, 0.3))


func _on_track_finished() -> void:
	finished = true
	SongClock.stop()
	results_label.text = "RUN COMPLETE\nmax chain %d — coins %d\n\ntap to return" \
		% [max_chain, coin_count]
	results_label.visible = true


func toast(msg: String) -> void:
	toast_label.text = msg
	var stamp := Time.get_ticks_msec()
	set_meta("toast_stamp", stamp)
	get_tree().create_timer(2.5).timeout.connect(
		func() -> void:
			if is_instance_valid(toast_label) and get_meta("toast_stamp") == stamp:
				toast_label.text = "")


func _back_to_menu() -> void:
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
