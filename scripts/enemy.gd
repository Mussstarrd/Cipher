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
	# Procedural alley trash can, feet at the origin.
	var metal := Color(0.58, 0.62, 0.68)
	var shade := Color(0.42, 0.45, 0.52)
	var lid := Color(0.5, 0.54, 0.61)
	# tapered body
	draw_polygon(PackedVector2Array([
		Vector2(-SIZE / 2.0, -SIZE + HEAD_H),
		Vector2(SIZE / 2.0, -SIZE + HEAD_H),
		Vector2(SIZE / 2.0 - 6.0, 0.0),
		Vector2(-SIZE / 2.0 + 6.0, 0.0),
	]), PackedColorArray([metal]))
	# ridges
	for i in 3:
		var x := -12.0 + i * 12.0
		draw_line(Vector2(x, -SIZE + HEAD_H + 4.0), Vector2(x * 0.85, -3.0), shade, 2.0)
	# lid (this slab is the bounce target / head zone)
	draw_rect(Rect2(-SIZE / 2.0 - 4.0, -SIZE, SIZE + 8.0, HEAD_H), lid)
	draw_rect(Rect2(-7.0, -SIZE - 6.0, 14.0, 6.0), shade)  # handle
	# a little spilled trash at the base
	draw_circle(Vector2(-SIZE / 2.0 - 2.0, -3.0), 4.0, Color(0.35, 0.5, 0.3))
	draw_circle(Vector2(SIZE / 2.0 + 1.0, -2.0), 3.0, Color(0.6, 0.55, 0.35))
