class_name RunnerCoin
extends Node2D
## Coin dropped on a miss; scatters and is briefly recollectable.
## Purely visual motion — coins are not judgment-critical, so frame delta
## is fine here (the no-delta rule covers judgment and spawning only).

const GRAVITY := 900.0

var _vel := Vector2.ZERO
var _age := 0.0
var _lifetime := 2.5
var _floor_y := 560.0


func launch(from: Vector2, vel: Vector2, lifetime: float) -> void:
	position = from
	_vel = vel
	_lifetime = lifetime
	_floor_y = from.y + 40.0


func collectable() -> bool:
	return _age > 0.25 and _age < _lifetime


func _process(delta: float) -> void:
	_age += delta
	_vel.y += GRAVITY * delta
	position += _vel * delta
	if position.y > _floor_y:
		position.y = _floor_y
		_vel = Vector2(_vel.x * 0.6, -_vel.y * 0.4)
	if _age >= _lifetime:
		queue_free()
		return
	queue_redraw()


func _draw() -> void:
	var alpha := 1.0
	if _age > _lifetime - 0.6:  # blink before vanishing
		alpha = 0.4 + 0.6 * float(int(_age * 10.0) % 2)
	draw_rect(Rect2(-7.0, -7.0, 14.0, 14.0), Color(0.98, 0.85, 0.25, alpha))
