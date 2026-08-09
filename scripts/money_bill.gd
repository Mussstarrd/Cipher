class_name MoneyBill
extends Node2D
## A bill (or coin) tossed from the crowd. Purely visual motion — frame
## delta is fine here, nothing judgment-critical.

var _vel := Vector2.ZERO
var _spin := 0.0
var _floor_y := 0.0
var _age := 0.0
var _is_coin := false


func toss(from: Vector2, to: Vector2, coin: bool) -> void:
	position = from
	_is_coin = coin
	_floor_y = to.y
	var dx := to.x - from.x
	_vel = Vector2(dx * 1.1, -420.0 - absf(dx) * 0.4)
	_spin = 6.0 if dx > 0.0 else -6.0


func _process(delta: float) -> void:
	_age += delta
	if position.y < _floor_y:
		_vel.y += 980.0 * delta
		position += _vel * delta
		rotation += _spin * delta
		if position.y >= _floor_y:
			position.y = _floor_y
			rotation = 0.2 if _spin > 0.0 else -0.2
	if _age > 4.0:
		queue_free()
		return
	if _age > 3.2:
		modulate.a = maxf(0.0, (4.0 - _age) / 0.8)
	queue_redraw()


func _draw() -> void:
	if _is_coin:
		draw_circle(Vector2.ZERO, 7.0, Color(0.85, 0.68, 0.18))
		draw_circle(Vector2.ZERO, 5.0, Color(0.98, 0.85, 0.25))
	else:
		draw_rect(Rect2(-12.0, -6.0, 24.0, 12.0), Color(0.35, 0.65, 0.35))
		draw_rect(Rect2(-11.0, -5.0, 22.0, 10.0), Color(0.45, 0.75, 0.45))
		draw_circle(Vector2.ZERO, 3.5, Color(0.3, 0.55, 0.3))
