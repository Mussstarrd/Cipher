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
	var c := Color(0.95, 0.72, 0.28)
	if _beat < knock_until_beat:
		c = Color(0.9, 0.25, 0.25)
	draw_rect(Rect2(-WIDTH / 2.0, -HEIGHT, WIDTH, HEIGHT), c)
	draw_rect(Rect2(WIDTH * 0.08, -HEIGHT * 0.82, 8.0, 8.0), Color(0.1, 0.1, 0.12))
