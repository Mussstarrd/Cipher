class_name Spectator
extends Node2D
## A procedural onlooker. Walks in from offscreen toward an assigned slot,
## bobs with the music, raises arms to cheer at higher hype levels.
## position is the point between their feet.

var slot_pos := Vector2.ZERO
var phase := 0.0
var seed_n := 0
var cheer := 0       # 0 = watch/bob, 1 = arms up on downbeats, 2 = going wild
var leaving := false

var _beat := 0.0


static func _hash01(n: int) -> float:
	return fposmod(sin(float(n) * 12.9898) * 43758.5453, 1.0)


func set_beat(beat: float) -> void:
	_beat = beat
	var target := slot_pos if not leaving else Vector2(slot_pos.x * 3.0, slot_pos.y)
	position = position.lerp(target, 0.05)
	queue_redraw()


func offscreen() -> bool:
	return leaving and (position.x < -100.0 or position.x > 3000.0)


func _draw() -> void:
	var skin := Color(0.85, 0.66, 0.5).lightened(_hash01(seed_n) * 0.2)
	var shirt := Color.from_hsv(_hash01(seed_n + 9), 0.55, 0.75)
	var pants := Color.from_hsv(_hash01(seed_n + 21), 0.4, 0.4)
	var scale_f := 0.8 + _hash01(seed_n + 5) * 0.25
	var bob := -4.0 * absf(sin((_beat + phase) * PI)) * (1.0 + 0.6 * cheer)
	var arms_up: bool = cheer >= 2 or (cheer == 1 and fposmod(_beat, 4.0) < 1.0)

	draw_set_transform(Vector2(0.0, 0.0), 0.0, Vector2(scale_f, scale_f))
	var hip := Vector2(0.0, -40.0 + bob)
	var shoulder := hip + Vector2(0.0, -30.0)
	draw_line(hip, Vector2(-9.0, 0.0), pants, 8.0)
	draw_line(hip, Vector2(9.0, 0.0), pants, 8.0)
	draw_rect(Rect2(hip.x - 13.0, shoulder.y, 26.0, hip.y - shoulder.y + 3.0), shirt)
	if arms_up:
		draw_line(shoulder + Vector2(-12.0, 3.0), shoulder + Vector2(-24.0, -26.0), shirt, 7.0)
		draw_line(shoulder + Vector2(12.0, 3.0), shoulder + Vector2(24.0, -26.0), shirt, 7.0)
	else:
		draw_line(shoulder + Vector2(-12.0, 3.0), shoulder + Vector2(-17.0, 26.0), shirt, 7.0)
		draw_line(shoulder + Vector2(12.0, 3.0), shoulder + Vector2(17.0, 26.0), shirt, 7.0)
	var head := shoulder + Vector2(0.0, -18.0)
	draw_circle(head, 13.0, skin)
	if arms_up:  # :D
		draw_circle(head + Vector2(0.0, 4.0), 4.0, Color.BLACK)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
