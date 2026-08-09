class_name RunnerEnemy
extends Node2D
## Single POC enemy type: a rectangle sitting on its chart beat.
## position is the enemy's feet-center point.

const SIZE := 48.0
const HEAD_H := 14.0

var beat := 0.0
var resolved := false  # bounced on, or already caused a miss


func body_rect() -> Rect2:
	return Rect2(position.x - SIZE / 2.0, position.y - SIZE, SIZE, SIZE)


## Spatial gate target: the head slab expanded by the configured threshold.
func head_zone(threshold_px: float) -> Rect2:
	return Rect2(
		position.x - SIZE / 2.0 - threshold_px,
		position.y - SIZE - threshold_px,
		SIZE + threshold_px * 2.0,
		HEAD_H + threshold_px * 2.0)


func squash() -> void:
	resolved = true
	scale = Vector2(1.35, 0.3)
	modulate.a = 0.5
	queue_redraw()


func consume_miss() -> void:
	resolved = true
	modulate = Color(0.55, 0.55, 0.55, 0.6)
	queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(-SIZE / 2.0, -SIZE, SIZE, SIZE), Color(0.85, 0.3, 0.32))
	draw_rect(Rect2(-SIZE / 2.0, -SIZE, SIZE, HEAD_H), Color(0.62, 0.18, 0.2))
