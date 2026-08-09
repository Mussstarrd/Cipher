class_name Dancer
extends Node2D
## The goofy street dancer, drawn procedurally facing the camera.
## Every pose is a pure function of the song beat and the current dance
## tier, so the choreography is beat-locked by construction.
## position is the point between his feet on the ground.

const SKIN := Color(0.92, 0.72, 0.55)
const SHIRT := Color(0.88, 0.36, 0.66)   # loud thrift-store shirt
const PANTS := Color(0.27, 0.32, 0.62)
const SHOE := Color(0.96, 0.95, 0.9)
const LIMB_W := 9.0

var tier := 0
var stumble_until := -999.0

var _beat := 0.0


func set_beat(beat: float) -> void:
	_beat = beat
	queue_redraw()


func is_stumbling() -> bool:
	return _beat < stumble_until


static func _hash01(n: int) -> float:
	return fposmod(sin(float(n) * 12.9898) * 43758.5453, 1.0)


## Two-segment limb from `a`; returns the hand/foot position.
func _limb(a: Vector2, ang1: float, l1: float, ang2: float, l2: float,
		col: Color) -> Vector2:
	var e := a + Vector2(cos(ang1), sin(ang1)) * l1
	var h := e + Vector2(cos(ang2), sin(ang2)) * l2
	draw_line(a, e, col, LIMB_W)
	draw_line(e, h, col, LIMB_W)
	return h


func _draw() -> void:
	var t := _beat
	var p := fposmod(t, 1.0)
	var n := int(floorf(t))
	var stumbling := is_stumbling()

	if stumbling:
		_draw_stumble(t)
		return
	if tier >= 4:
		_draw_breakdance(t)
		return

	var bob := -5.0 * absf(sin(p * PI)) * (1.0 + 0.5 * tier)
	var sway := 0.0
	var head_shake := 0.0
	# Arm angles: PI/2 = hanging straight down.
	var l_sh := PI / 2.0 + 0.25
	var l_el := PI / 2.0 + 0.15
	var r_sh := PI / 2.0 - 0.25
	var r_el := PI / 2.0 - 0.15

	match tier:
		0:  # head-nod bounce
			sway = sin(t * PI) * 3.0
			l_sh += 0.08 * sin(t * TAU)
			r_sh -= 0.08 * sin(t * TAU)
		1:  # twitch dance: per-beat random jerks, snapped early in the beat
			var snap := clampf(p * 6.0, 0.0, 1.0)
			l_sh = lerp_angle(PI / 2.0 + (_hash01(n - 1) - 0.5) * 2.4,
				PI / 2.0 + (_hash01(n) - 0.5) * 2.4, snap)
			r_sh = lerp_angle(PI / 2.0 - (_hash01(n + 40) - 0.5) * 2.4,
				PI / 2.0 - (_hash01(n + 41) - 0.5) * 2.4, snap)
			l_el = l_sh + 1.2
			r_el = r_sh - 1.2
			head_shake = (_hash01(n + 80) - 0.5) * 8.0
		2:  # harlem shake: everything flails
			l_sh = PI / 2.0 - 0.9 + sin(t * TAU * 2.0) * 1.5
			r_sh = PI / 2.0 + 0.9 + sin(t * TAU * 2.0 + PI) * 1.5
			l_el = l_sh - 1.0 + sin(t * TAU * 4.0) * 0.8
			r_el = r_sh + 1.0 + sin(t * TAU * 4.0 + 1.7) * 0.8
			head_shake = sin(t * TAU * 2.0) * 9.0
			bob *= 1.6
			sway = sin(t * TAU) * 8.0
		3:  # zombie dance: arms out stiff, lurching side to side
			var lurch := sin(t * PI)
			sway = lurch * 14.0
			l_sh = PI + 0.12 * sin(t * TAU)          # out to the left
			l_el = l_sh + 0.15
			r_sh = 0.0 - 0.12 * sin(t * TAU)         # out to the right
			r_el = r_sh - 0.15
			head_shake = lurch * 5.0
			bob = -3.0 * absf(sin(p * PI))

	var hip := Vector2(sway, -52.0 + bob)
	var shoulder := hip + Vector2(head_shake * 0.4, -38.0)

	# legs: feet planted wide, knees pump with the bob
	var l_foot := Vector2(-14.0 + sway * 0.3, 0.0)
	var r_foot := Vector2(14.0 + sway * 0.3, 0.0)
	draw_line(hip + Vector2(-6, 0), l_foot + Vector2(0, -4), PANTS, LIMB_W + 2.0)
	draw_line(hip + Vector2(6, 0), r_foot + Vector2(0, -4), PANTS, LIMB_W + 2.0)
	draw_rect(Rect2(l_foot.x - 11.0, -8.0, 22.0, 8.0), SHOE)
	draw_rect(Rect2(r_foot.x - 11.0, -8.0, 22.0, 8.0), SHOE)

	# torso
	draw_rect(Rect2(hip.x - 17.0, shoulder.y, 34.0, hip.y - shoulder.y + 4.0), SHIRT)

	# arms
	_limb(shoulder + Vector2(-16.0, 4.0), l_sh, 24.0, l_el, 22.0, SHIRT)
	_limb(shoulder + Vector2(16.0, 4.0), r_sh, 24.0, r_el, 22.0, SHIRT)

	# head + face
	var head := shoulder + Vector2(head_shake, -22.0)
	draw_circle(head, 17.0, SKIN)
	draw_circle(head + Vector2(-6.0, -3.0), 2.6, Color.BLACK)
	draw_circle(head + Vector2(6.0, -3.0), 2.6, Color.BLACK)
	# big goofy grin
	draw_arc(head + Vector2(0.0, 3.0), 8.0, 0.3, PI - 0.3, 10, Color.BLACK, 2.5)


func _draw_breakdance(t: float) -> void:
	# Spinning on the floor: whole body rotates around a point near the
	# ground, legs kicking. Peak comedy per line of code.
	var rot := t * PI  # half a revolution per beat
	draw_set_transform(Vector2(0.0, -26.0), rot, Vector2.ONE)
	# body horizontal-ish
	draw_rect(Rect2(-14.0, -30.0, 28.0, 34.0), SHIRT)
	# supporting arm to the floor
	_limb(Vector2(0.0, 2.0), PI / 2.0 - 0.3, 20.0, PI / 2.0, 16.0, SHIRT)
	# legs kicking in a V
	var kick := sin(t * TAU * 2.0) * 0.5
	draw_line(Vector2(0.0, -28.0), Vector2(0.0, -28.0)
		+ Vector2(cos(-PI / 2.0 - 0.7 + kick), sin(-PI / 2.0 - 0.7 + kick)) * 40.0,
		PANTS, LIMB_W + 2.0)
	draw_line(Vector2(0.0, -28.0), Vector2(0.0, -28.0)
		+ Vector2(cos(-PI / 2.0 + 0.7 - kick), sin(-PI / 2.0 + 0.7 - kick)) * 40.0,
		PANTS, LIMB_W + 2.0)
	var head := Vector2(0.0, 16.0)
	draw_circle(head, 15.0, SKIN)
	draw_circle(head + Vector2(-5.0, 2.0), 2.4, Color.BLACK)
	draw_circle(head + Vector2(5.0, 2.0), 2.4, Color.BLACK)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


func _draw_stumble(t: float) -> void:
	# Tripping over his own shoes: tilted body, windmilling arms.
	var fall := clampf((stumble_until - t) / 1.5, 0.0, 1.0)
	var tilt := (1.0 - fall) * 0.2 + 0.35
	draw_set_transform(Vector2(0.0, 0.0), tilt * sin(t * TAU) * 0.4 + 0.3, Vector2.ONE)
	var hip := Vector2(6.0, -46.0)
	var shoulder := hip + Vector2(10.0, -36.0)
	draw_line(hip, Vector2(-16.0, 0.0), PANTS, LIMB_W + 2.0)
	draw_line(hip, Vector2(22.0, -14.0), PANTS, LIMB_W + 2.0)  # leg in the air
	draw_rect(Rect2(Vector2(-16.0 - 11.0, -8.0), Vector2(22.0, 8.0)), SHOE)
	draw_rect(Rect2(hip.x - 16.0, shoulder.y, 32.0, hip.y - shoulder.y + 4.0), SHIRT)
	var wind := t * TAU * 3.0
	_limb(shoulder + Vector2(-14.0, 4.0), wind, 24.0, wind + 0.8, 22.0, SHIRT)
	_limb(shoulder + Vector2(14.0, 4.0), wind + PI, 24.0, wind + PI + 0.8, 22.0, SHIRT)
	var head := shoulder + Vector2(4.0, -22.0)
	draw_circle(head, 17.0, SKIN)
	# X_X eyes
	for s in [-1.0, 1.0]:
		var e := head + Vector2(6.0 * s, -3.0)
		draw_line(e + Vector2(-3, -3), e + Vector2(3, 3), Color.BLACK, 2.0)
		draw_line(e + Vector2(-3, 3), e + Vector2(3, -3), Color.BLACK, 2.0)
	draw_circle(head + Vector2(0.0, 6.0), 3.5, Color.BLACK)  # yelling mouth
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
