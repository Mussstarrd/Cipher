class_name RunnerPlayer
extends Node2D
## The auto-running character. Horizontal position and the jump arc are both
## pure functions of the song beat — no gravity simulation, no frame-delta
## physics. position is the character's feet-center point.

const WIDTH := 40.0
const HEIGHT := 56.0

var ground_y := 520.0
var jump_height := 150.0
var arc_beats := 1.0

var air := false
var arc_start_beat := 0.0
var land_beat := -999.0
var knock_until_beat := -999.0

var _beat := 0.0


## Start a beat-locked arc (jump from ground or rebound off an enemy).
func begin_arc(beat: float) -> void:
	air = true
	arc_start_beat = beat


## Called every frame by the runner with the current song beat.
func advance(beat: float, x: float) -> void:
	_beat = beat
	position.x = x
	var y := ground_y
	if air:
		var t := (beat - arc_start_beat) / arc_beats
		if t >= 1.0:
			air = false
			land_beat = arc_start_beat + arc_beats
		else:
			y = ground_y - jump_height * sin(PI * clampf(t, 0.0, 1.0))
	position.y = y
	queue_redraw()


## Bounce eligibility: airborne, or within a short grace after the arc
## landed (covers the exact-landing race when enemies sit 1 beat apart).
func can_bounce(beat: float, grace_beats: float) -> bool:
	return air or (beat - land_beat) <= grace_beats


func feet() -> Vector2:
	return position


func body_rect() -> Rect2:
	return Rect2(position.x - WIDTH / 2.0, position.y - HEIGHT, WIDTH, HEIGHT)


func _draw() -> void:
	# Procedural lizard, feet at the origin. Colors flash red on knockback.
	var body := Color(0.42, 0.75, 0.34)
	var belly := Color(0.71, 0.88, 0.52)
	var dark := Color(0.28, 0.52, 0.23)
	if _beat < knock_until_beat:
		body = Color(0.9, 0.28, 0.26)
		belly = Color(1.0, 0.6, 0.55)
		dark = Color(0.6, 0.18, 0.16)
	var run := sin(_beat * TAU)  # gait cycle locked to the beat

	# tail (whips with the gait)
	draw_polygon(PackedVector2Array([
		Vector2(-14.0, -30.0),
		Vector2(-40.0, -12.0 + run * 4.0),
		Vector2(-12.0, -16.0),
	]), PackedColorArray([dark]))
	# body
	draw_rect(Rect2(-16.0, -36.0, 34.0, 22.0), body)
	draw_rect(Rect2(-16.0, -18.0, 34.0, 8.0), belly)
	# back spikes
	for i in 3:
		var sx := -10.0 + i * 10.0
		draw_polygon(PackedVector2Array([
			Vector2(sx, -36.0), Vector2(sx + 5.0, -42.0), Vector2(sx + 10.0, -36.0),
		]), PackedColorArray([dark]))
	# head and snout
	draw_rect(Rect2(12.0, -50.0, 18.0, 20.0), body)
	draw_rect(Rect2(26.0, -44.0, 12.0, 12.0), body)
	draw_rect(Rect2(26.0, -36.0, 12.0, 4.0), belly)
	draw_circle(Vector2(22.0, -44.0), 3.2, Color(0.07, 0.09, 0.07))
	draw_circle(Vector2(23.0, -45.0), 1.1, Color(0.95, 0.95, 0.9))
	# legs: tucked in the air, scissoring on the ground
	if air:
		draw_rect(Rect2(-10.0, -12.0, 8.0, 8.0), dark)
		draw_rect(Rect2(6.0, -12.0, 8.0, 8.0), dark)
	else:
		draw_rect(Rect2(-12.0 + 5.0 * run, -10.0, 8.0, 10.0), dark)
		draw_rect(Rect2(6.0 - 5.0 * run, -10.0, 8.0, 10.0), dark)
